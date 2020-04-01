using System;
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
    public class MiningTxhubBenchmark : MiningBenchmarkTestBase
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
            await AddTransactions(TransactionCount);
        }

        [Benchmark]
        public async Task MineWithTxhubAsync()
        {
            var txCount = 0;
            Block block;
            var preBlockHash = _chain.BestChainHash;
            var preBlockHeight = _chain.BestChainHeight;

            while (txCount < TransactionCount)
            {
                block = await _minerService.MineAsync(preBlockHash, preBlockHeight,
                    TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(4000));
                txCount += block.TransactionIds.Count();
                _transactionIdList.AddRange(block.TransactionIds.ToList());

                // if (!block.TransactionIds.Any())
                // {
                //     // var txCountInPool = await TxHub.GetAllTransactionCountAsync();
                //     // Console.WriteLine($"Transaction count in pool: {txCountInPool}");
                // }
                // else 
                //     Console.WriteLine($"Block transaction count: {block.TransactionIds.Count()}");

                // _chain.BestChainHash = block.GetHash();
                // _chain.BestChainHeight = block.Height;
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

            Console.WriteLine($"Total block transaction count: {txCount}");
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            // await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            // var transactionIds = _transactions.Select(t => t.GetHash()).ToList();
            await _transactionManager.RemoveTransactionsAsync(_transactionIdList);
            // await _transactionResultManager.RemoveTransactionResultsAsync(transactionIds, _block.GetHash());
            // await _transactionResultManager.RemoveTransactionResultsAsync(transactionIds,
            //     _block.Header.GetPreMiningHash());

            await TxHub.CleanTransactionsAsync(_transactionIdList);

            await TxHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = _chain.BestChainHash,
                BlockHeight = _chain.BestChainHeight
            });
            _transactionIdList.Clear();
        }

        private async Task AddTransactions(int txCount)
        {
            var txList = await GenerateTransferTransaction(txCount);
            var transactionsReceivedEvent = new TransactionsReceivedEvent
            {
                Transactions = txList
            };

            // Console.WriteLine($"add {TransactionCount} tx");
            await TxHub.AddTransactionsAsync(transactionsReceivedEvent);
        }

        private Task CreateTransactionGenerationTasks(int count)
        {
            int i = 0;
            while (i++ < count)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await AddTransactions(1);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                });
            }

            return Task.CompletedTask;
        }
    }
}