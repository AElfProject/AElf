using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Benchmark
{
    public class FibonacciTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private ISmartContractAddressService _smartContractAddressService;
        private IAccountService _accountService;
        private ITransactionResultService _transactionResultService;
        private ITransactionResultManager _transactionResultManager;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private IBlockExecutingService _blockExecutingService;
        private OSTestHelper _osTestHelper;

        private Transaction _transaction;
        private Block _block;
        private Address _contractAddress;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _accountService = GetRequiredService<IAccountService>();
            _transactionResultService = GetRequiredService<ITransactionResultService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();

            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var transaction = _osTestHelper.GenerateTransaction(Address.Generate(), basicContractZero,
                nameof(ISmartContractZero.DeploySmartContract), new ContractDeploymentInput()
                {
                    Category = 0,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(PerformanceTestContract.PerformanceTestContract)
                        .Assembly.Location))
                });

            var signature = await _accountService.SignAsync(transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            await _osTestHelper.MinedOneBlock();

            var txResult = await _transactionResultService.GetTransactionResultAsync(transaction.GetHash());

            _contractAddress = Address.Parser.ParseFrom(txResult.ReturnValue);
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            var chain = await _blockchainService.GetChainAsync();

            _block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = chain.Id,
                    Height = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };

            _transaction = _osTestHelper.GenerateTransaction(Address.Generate(), _contractAddress,
                "Fibonacci", new UInt64Value
                {
                    Value = 16
                });
        }

        [Benchmark]
        public async Task Fibonacci16()
        {
            _block = await _blockExecutingService.ExecuteBlockAsync(_block.Header, new List<Transaction> {_transaction});
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            _transactionResultManager.RemoveTransactionResultAsync(_transaction.GetHash(), _block.GetHash());
            _transactionResultManager.RemoveTransactionResultAsync(_transaction.GetHash(),
                _block.Header.GetPreMiningHash());
        }
    }
}