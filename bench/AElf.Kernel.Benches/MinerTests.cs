using System;
using System.Collections.Generic;
using System.Diagnostics;
using AElf.BenchBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Kernel.Benches
{
    public class MinerTests: BenchBaseTest<KernelBenchAElfModule>
    {
        private IBlockchainService _blockchainService;
        private IMinerService _minerService;
        private ITxHub _txHub;
        private IBlockAttachService _blockAttachService;
        private OSTestHelper _osTestHelper;
        
        private Counter _counter;

        private Block _block;
        
        public MinerTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _minerService = GetRequiredService<IMinerService>();
            _txHub = GetRequiredService<ITxHub>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
                
            _counter = context.GetCounter("TestCounter");

            AsyncHelper.RunSync(async () =>
            {
                var transactions = await _osTestHelper.GenerateTransferTransactions(1000);

                await _osTestHelper.BroadcastTransactions(transactions);
            });
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 5, RunMode = RunMode.Iterations,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        public void MineTest()
        {
            AsyncHelper.RunSync(async () =>
            {
                var chain = await _blockchainService.GetChainAsync();
                _block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                    DateTime.UtcNow, TimeSpan.FromMilliseconds(4000));
            });
            _counter.Increment();
        }

        [PerfCleanup]
        public void Cleanup()
        {
            AsyncHelper.RunSync(async () =>
            {
                await _blockAttachService.AttachBlockAsync(_block);
                var chain = await _blockchainService.GetChainAsync();
                await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            });
        }
    }
}