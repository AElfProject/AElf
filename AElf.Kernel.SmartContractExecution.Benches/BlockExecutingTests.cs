using System;
using System.Collections.Generic;
using System.Diagnostics;
using AElf.BenchBase;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using Google.Protobuf.WellKnownTypes;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Benches
{
    public class BlockExecutingTests : BenchBaseTest<ExecutionBenchAElfModule>
    {
        private IBlockExecutingService _blockExecutingService;
        private IBlockchainService _blockchainService;
        private OSTestHelper _osTestHelper;

        private List<Transaction> _transactions;
        private Block _block;
        
        private Counter _counter;
        
        public BlockExecutingTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
            
            SetTestOutputHelper(output);
        }
        
        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            
            _counter = context.GetCounter("TestCounter");

            AsyncHelper.RunSync(async () =>
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
            
                _transactions = await _osTestHelper.GenerateTransferTransactions(1000);
            });
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 5, RunMode = RunMode.Iterations,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        public void ExecuteBlock()
        {
            AsyncHelper.RunSync(() => _blockExecutingService.ExecuteBlockAsync(_block.Header, _transactions));
            _counter.Increment();
        }
    }
}