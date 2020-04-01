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
    public class MiningTxhubBenchmark : MiningWithTransactionsBenchmarkBase
    {
        private readonly IMinerService _minerService;
        private readonly ITransactionManager _transactionManager;

        public MiningTxhubBenchmark()
        {
            _minerService = GetRequiredService<IMinerService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
        }

        private Chain _chain;
        private readonly List<Hash> _transactionIdList = new List<Hash>();

        [Params(3000, 5000, 10000)] public int TransactionCount;

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
        public async Task MineWithTxhubAsync()
        {
            var txCount = 0;
            var preBlockHash = _chain.BestChainHash;
            var preBlockHeight = _chain.BestChainHeight;

            while (txCount < TransactionCount)
            {
                var block = await _minerService.MineAsync(preBlockHash, preBlockHeight,
                    TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(4000));
                txCount += block.TransactionIds.Count();
                _transactionIdList.AddRange(block.TransactionIds.ToList());
                await BlockchainService.SetBestChainAsync(_chain, preBlockHeight, preBlockHash);
                await TxHub.HandleBlockAcceptedAsync(new BlockAcceptedEvent
                {
                    Block = block
                });
                await TxHub.HandleBestChainFoundAsync(new BestChainFoundEventData
                {
                    BlockHash = preBlockHash,
                    BlockHeight = preBlockHeight
                });
            }
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _transactionManager.RemoveTransactionsAsync(_transactionIdList);
            await TxHub.CleanTransactionsAsync(_transactionIdList);

            await TxHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = _chain.BestChainHash,
                BlockHeight = _chain.BestChainHeight
            });
            _transactionIdList.Clear();
        }

        private async Task AddTransactionsToTxHub(int txCount)
        {
            var txList = await GenerateTransferTransactionsAsync(txCount);
            var transactionsReceivedEvent = new TransactionsReceivedEvent
            {
                Transactions = txList
            };
            await TxHub.AddTransactionsAsync(transactionsReceivedEvent);
        }
    }
}