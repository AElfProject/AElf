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
        private IBlockchainService _blockchainService;
        private IMinerService _minerService;
        private OSTestHelper _osTestHelper;
                        
        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _minerService = GetRequiredService<IMinerService>();
            
            var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
            await _osTestHelper.BroadcastTransactions(transactions);
        }
        
        [Benchmark]
        public async Task MineBlockTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                DateTime.UtcNow, TimeSpan.FromMilliseconds(4000));
        }
    }
}