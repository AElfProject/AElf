using System.Diagnostics;
using AElf.BenchBase;
using AElf.OS;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Kernel.Core.Benches
{
    public class TransactionVerifySignatureTests: BenchBaseTest<KernelCoreBenchAElfModule>
    {    
        private OSTestHelper _osTestHelper;
        
        private Counter _counter;

        private Transaction _transaction;
        public TransactionVerifySignatureTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _osTestHelper = GetRequiredService<OSTestHelper>();
                
            _counter = context.GetCounter("TestCounter");

            AsyncHelper.RunSync(async () =>
            {
                _transaction = await _osTestHelper.GenerateTransferTransaction();
            });
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 5, RunMode = RunMode.Iterations,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        public void VerifySignatureTest()
        {
            _transaction.VerifySignature();
            _counter.Increment();
        }
    }
}