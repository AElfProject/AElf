using System.Collections.Generic;
using System.Diagnostics;
using AElf.BenchBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Benches
{
    public class BlockAttachTests: BenchBaseTest<ExecutionBenchAElfModule>
    {
        private IBlockchainService _blockchainService;
        private IBlockAttachService _blockAttachService;
        private OSTestHelper _osTestHelper;
        
        private Counter _counter;
        
        private Block _block;
        
        public BlockAttachTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            
            _counter = context.GetCounter("TestCounter");

            AsyncHelper.RunSync(async () =>
            {
                var chain = await _blockchainService.GetChainAsync();

                var transactions = await _osTestHelper.GenerateTransferTransactions(1000);

                _block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            });
        }
        
        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 5, RunMode = RunMode.Iterations,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        public void AttachBlockTest()
        {
            AsyncHelper.RunSync(() => _blockAttachService.AttachBlockAsync(_block));
            _counter.Increment();
        }
    }
}