using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Benchmark.PerformanceTestContract;
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
    [MarkdownExporterAttribute.GitHub]
    public class LoopExpNop1MTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private OSTestHelper _osTestHelper;

        private Transaction _transaction;
        private Block _block;
        private Address _contractAddress;
        private Chain _chain;
        private TransactionTrace _transactionTrace;

        private const int _executeResult = 15;
        
        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _transactionReadOnlyExecutionService = GetRequiredService<ITransactionReadOnlyExecutionService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();

            _contractAddress = await _osTestHelper.DeployContract<PerformanceTestContract.PerformanceTestContract>();
            _chain = await _blockchainService.GetChainAsync();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            _transaction = _osTestHelper.GenerateTransaction(Address.Generate(), _contractAddress,
                nameof(PerformanceTestContract.PerformanceTestContract.LoopExpNop), new PerformanceTesteInput()
                {
                    Exponent = 0,
                    Seed = _executeResult,
                    N = 1000000
                });
        }

        [Benchmark]
        public async Task LoopExpNop1M()
        {
            _transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = _chain.BestChainHash,
                    BlockHeight = _chain.BestChainHeight
                },
                _transaction,
                DateTime.UtcNow);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            var calResult = UInt64Value.Parser.ParseFrom(_transactionTrace.ReturnValue).Value;
            if (calResult != _executeResult)
            {
                throw new Exception("execute fail");
            }
        }
    }
}