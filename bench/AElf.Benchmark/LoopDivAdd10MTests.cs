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
    public class LoopDivAdd10MTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private OSTestHelper _osTestHelper;

        private Transaction _transaction;
        private Block _block;
        private Address _contractAddress;
        private Chain _chain;
        private TransactionTrace _transactionTrace;

        private const double executeResult = 501.67224080267556; 
        
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
                nameof(PerformanceTestContract.PerformanceTestContract.LoopDivAdd), new DivAddTestInput()
                {
                    X = 100,
                    Y = 300,
                    K = 500,
                    N = 10000000
                });
        }

        [Benchmark]
        public async Task LoopDivAdd10M()
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
            var calResult = DoubleValue.Parser.ParseFrom(_transactionTrace.ReturnValue).Value;
            if (calResult != executeResult)
            {
                throw new Exception("execute fail");
            }
        }
    }
}