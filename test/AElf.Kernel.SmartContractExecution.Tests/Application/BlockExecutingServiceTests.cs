using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class BlockExecutingServiceTests : SmartContractExecutionTestBase
    {
        private readonly BlockExecutingService _blockExecutingService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly SmartContractExecutionHelper _smartContractExecutionHelper;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;

        public BlockExecutingServiceTests()
        {
            _blockExecutingService = GetRequiredService<BlockExecutingService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _smartContractExecutionHelper = GetRequiredService<SmartContractExecutionHelper>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _systemTransactionExtraDataProvider = GetRequiredService<ISystemTransactionExtraDataProvider>();
        }

        [Fact]
        public async Task Execute_Block_NonCancellable_Without_SystemTransactionCount()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();
            var blockHeader = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash).Header;
            var transactions = GetTransactions();
            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, transactions);
            await CheckBlockExecutedSetAsync(blockExecutedSet, 2); 
        }
        
        [Fact]
        public async Task Execute_Block_NonCancellable_With_SystemTransactionCount()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();
            var blockHeader = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash).Header;
            _systemTransactionExtraDataProvider.SetSystemTransactionCount(1, blockHeader);
            var transactions = GetTransactions();
            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, transactions);
            await CheckBlockExecutedSetAsync(blockExecutedSet, 2); 
        }

        [Fact]
        public async Task Execute_Block_Cancellable_Empty()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();

            var transactions = GetTransactions();

            var blockHeader = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash).Header;
            var nonCancellableTxs = new[] {transactions[0]};
            var cancellableTxs = new Transaction[0];

            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, nonCancellableTxs,
                cancellableTxs, CancellationToken.None);
            await CheckBlockExecutedSetAsync(blockExecutedSet, 1);
        }

        [Fact]
        public async Task Execute_Block_Cancellable_Cancelled()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();

            var blockHeader = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash).Header;
            var transactions = GetTransactions();
            var nonCancellableTxs = new[] {transactions[0]};
            var cancellableTxs = new[] {transactions[1]};
            var cancelToken = new CancellationTokenSource();
            cancelToken.Cancel();

            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, nonCancellableTxs,
                cancellableTxs, cancelToken.Token);
            await CheckBlockExecutedSetAsync(blockExecutedSet, 1);
        }

        [Fact]
        public async Task Execute_Block_Cancellable()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();

            var blockHeader = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash).Header;
            var transactions = GetTransactions();
            var nonCancellableTxs = new[] {transactions[0]};
            var cancellableTxs = new[] {transactions[1]};

            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, nonCancellableTxs,
                cancellableTxs, CancellationToken.None);
            await CheckBlockExecutedSetAsync(blockExecutedSet, 2);
        }

        private async Task CheckBlockExecutedSetAsync(BlockExecutedSet blockExecutedSet, int transactionCount)
        {
            blockExecutedSet.Block.Body.TransactionIds.Count.ShouldBe(transactionCount);
            blockExecutedSet.TransactionResultMap.Values.Select(t => t.Status)
                .ShouldAllBe(status => status == TransactionResultStatus.Mined);
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(blockExecutedSet.GetHash());
            blockStateSet.ShouldNotBeNull();
            var transactionResults = await _transactionResultManager.GetTransactionResultsAsync(
                blockExecutedSet.TransactionIds.ToList(),
                blockExecutedSet.GetHash());
            transactionResults.Count.ShouldBe(transactionCount);
            foreach (var transactionResult in transactionResults)
            {
                blockExecutedSet.TransactionResultMap[transactionResult.TransactionId].ShouldBe(transactionResult);
            }
        }

        private List<Transaction> GetTransactions()
        {
            return new List<Transaction>
            {
                new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = _smartContractAddressService.GetZeroSmartContractAddress(),
                    MethodName = nameof(ACS0Container.ACS0Stub.GetContractInfo),
                    Params = _smartContractAddressService.GetZeroSmartContractAddress().ToByteString()
                },
                new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = _smartContractAddressService.GetZeroSmartContractAddress(),
                    MethodName = nameof(ACS0Container.ACS0Stub.GetContractHash),
                    Params = _smartContractAddressService.GetZeroSmartContractAddress().ToByteString()
                }
            };
        }
    }
}