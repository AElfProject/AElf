using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class FullBlockchainExecutingServiceTests : SmartContractExecutionTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly SmartContractExecutionHelper _smartContractExecutionHelper;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public FullBlockchainExecutingServiceTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _smartContractExecutionHelper = GetRequiredService<SmartContractExecutionHelper>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task ExecuteBlocks_Success()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();

            var block = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash);
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = _smartContractAddressService.GetZeroSmartContractAddress(),
                    MethodName = nameof(ACS0Container.ACS0Stub.GetContractInfo),
                    Params = _smartContractAddressService.GetZeroSmartContractAddress().ToByteString()
                }
            };
            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockStateSetManger.RemoveBlockStateSetsAsync(new List<Hash> {blockExecutedSet.GetHash()});
            await _blockchainService.AddTransactionsAsync(transactions);
            await ExecuteBlocksAsync(new List<Block>{blockExecutedSet.Block});
        }
        
        [Fact]
        public async Task ExecuteBlocks_Success_WithExistBlockStateSet()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();

            var block = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash);
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = _smartContractAddressService.GetZeroSmartContractAddress(),
                    MethodName = nameof(ACS0Container.ACS0Stub.GetContractInfo),
                    Params = _smartContractAddressService.GetZeroSmartContractAddress().ToByteString()
                }
            };
            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await ExecuteBlocksAsync(new List<Block>{blockExecutedSet.Block});
        }

        private async Task ExecuteBlocksAsync(List<Block> blockList)
        {
            foreach (var block in blockList)
            {
                await _blockchainService.AddBlockAsync(block);
            }
            
            var executionResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAsync(blockList);

            executionResult.SuccessBlockExecutedSets.Count.ShouldBe(blockList.Count());
            for (var i = 0; i < blockList.Count; i++)
            {
                executionResult.SuccessBlockExecutedSets[i].GetHash().ShouldBe(blockList[i].GetHash());
            }
            executionResult.ExecutedFailedBlocks.Count.ShouldBe(0);
        }
    }
}