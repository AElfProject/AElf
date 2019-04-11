using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using BenchmarkDotNet.Attributes;
using Volo.Abp.Threading;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class MinerTests: BenchmarkTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IMinerService _minerService;
        private readonly ITxHub _txHub;
        private readonly IBlockAttachService _blockAttachService;
        private readonly OSTestHelper _osTestHelper;
        
        private Block _block;

        public MinerTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _minerService = GetRequiredService<IMinerService>();
            _txHub = GetRequiredService<ITxHub>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
        }
                
        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);

            await _osTestHelper.BroadcastTransactions(transactions);
        }

        [Benchmark]
        public async Task MineBlockTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            _block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                DateTime.UtcNow, TimeSpan.FromMilliseconds(4000));
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _blockAttachService.AttachBlockAsync(_block);
            var chain = await _blockchainService.GetChainAsync();
            await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });
        }
    }
}