using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.TransactionPool;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class MiningTxHubBenchmark : MiningWithTransactionsBenchmarkBase
    {
        private readonly IMinerService _minerService;
        private readonly ITransactionManager _transactionManager;

        public MiningTxHubBenchmark()
        {
            _minerService = GetRequiredService<IMinerService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
        }

        private Chain _chain;
        private readonly List<Hash> _transactionIdList = new List<Hash>();

        [Params(1000, 3000)] public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            await InitializeChainAsync();
            _chain = await BlockchainService.GetChainAsync();
        }


        [IterationSetup]
        public async Task IterationSetup()
        {
            await AddTransactionsToTxHub(TransactionCount);
        }

        [Benchmark]
        public async Task MineWithTxHubAsync()
        {
            var txCount = 0;
            var preBlockHash = _chain.BestChainHash;
            var preBlockHeight = _chain.BestChainHeight;

            while (txCount < TransactionCount)
            {
                var blockExecutedSet = await _minerService.MineAsync(preBlockHash, preBlockHeight,
                    TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(4000));
                txCount += blockExecutedSet.TransactionIds.Count();
                _transactionIdList.AddRange(blockExecutedSet.TransactionIds.ToList());
                await BlockchainService.SetBestChainAsync(_chain, preBlockHeight, preBlockHash);
                await TransactionPoolService.CleanByTransactionIdsAsync(blockExecutedSet.TransactionIds);
                await TransactionPoolService.UpdateTransactionPoolByBestChainAsync(preBlockHash, preBlockHeight);
            }
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _transactionManager.RemoveTransactionsAsync(_transactionIdList);
            await TransactionPoolService.CleanByTransactionIdsAsync(_transactionIdList);

            await TransactionPoolService.UpdateTransactionPoolByBestChainAsync(_chain.BestChainHash,
                _chain.BestChainHeight);
            _transactionIdList.Clear();
        }

        private async Task AddTransactionsToTxHub(int txCount)
        {
            var txList = await GenerateTransferTransactionsAsync(txCount);
            await TransactionPoolService.AddTransactionsAsync(txList);
        }
    }
}