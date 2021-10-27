using System;
using System.Threading.Tasks;
using AElf.Benchmark.PerformanceTestContract;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;
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
        private Address _contractAddress;
        private Chain _chain;
        private TransactionTrace _transactionTrace;

        private const int ExecuteResult = 15;

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
        public void IterationSetup()
        {
            _transaction = _osTestHelper.GenerateTransaction(SampleAddress.AddressList[0], _contractAddress,
                nameof(PerformanceTestContract.PerformanceTestContract.LoopExpNop), new PerformanceTesteInput()
                {
                    Exponent = 0,
                    Seed = ExecuteResult,
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
                TimestampHelper.GetUtcNow());
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            var calResult = UInt64Value.Parser.ParseFrom(_transactionTrace.ReturnValue).Value;
            if (calResult != ExecuteResult)
            {
                throw new Exception("execute fail");
            }
        }
    }
}