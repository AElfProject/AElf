using System.Collections.Generic;
using System.Diagnostics;
using AElf.BenchBase;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Kernel.TransactionPool.Benches
{
    public class TxHubTransactionsReceiveTests : BenchBaseTest<TransactionPoolBenchAElfModule>
    {
        private ITxHub _txHub;
        private OSTestHelper _osTestHelper;
        
        private Counter _counter;

        private List<Transaction> _transactions;
        
        public TxHubTransactionsReceiveTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _txHub = GetRequiredService<ITxHub>();
                
            _counter = context.GetCounter("TestCounter");

            AsyncHelper.RunSync(async () =>
            {
                _transactions = await _osTestHelper.GenerateTransferTransactions(1000);
            });
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 5, RunMode = RunMode.Iterations,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        public void HandleTransactionsReceivedTest()
        {
            AsyncHelper.RunSync(async () =>
            {
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = _transactions
                });
            });
            _counter.Increment();
        }
    }
}