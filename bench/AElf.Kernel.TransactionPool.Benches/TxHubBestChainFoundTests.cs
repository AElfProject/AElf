using System.Collections.Generic;
using System.Diagnostics;
using AElf.BenchBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Kernel.TransactionPool.Benches
{
    public class TxHubBestChainFoundTests: BenchBaseTest<TransactionPoolBenchAElfModule>
    {
        private ITxHub _txHub;
        private IBlockchainService _blockchainService;
        private OSTestHelper _osTestHelper;
        
        private Counter _counter;
        
        public TxHubBestChainFoundTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _txHub = GetRequiredService<ITxHub>();
            _blockchainService = GetRequiredService<IBlockchainService>();

            _counter = context.GetCounter("TestCounter");

            AsyncHelper.RunSync(async () =>
            {
                var transactions = await _osTestHelper.GenerateTransferTransactions(1000);

                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = transactions
                });

                await _osTestHelper.MinedOneBlock();
            });
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 5, RunMode = RunMode.Iterations,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        public void HandleBestChainFoundTest()
        {
            AsyncHelper.RunSync(async () =>
            {
                var chain = await _blockchainService.GetChainAsync();
                await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            });
            _counter.Increment();
        }
    }
}