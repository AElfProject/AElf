using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class MinerTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private IMinerService _minerService;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private ITransactionPoolService _transactionPoolService;
        private ITransactionManager _transactionManager;
        private OSTestHelper _osTestHelper;

        private Chain _chain;
        private Block _block;
        private List<Transaction> _transactions;

        [Params(1, 10, 100, 1000, 3000, 5000)] public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _minerService = GetRequiredService<IMinerService>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _transactionPoolService = GetRequiredService<ITransactionPoolService>();

            _transactions = new List<Transaction>();
            _chain = await _blockchainService.GetChainAsync();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            _transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
            await _osTestHelper.BroadcastTransactions(_transactions);
        }

        [Benchmark]
        public async Task MineBlockTest()
        {
            _block = (await _minerService.MineAsync(_chain.BestChainHash, _chain.BestChainHeight,
                TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromSeconds(4))).Block;
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            var transactionIds = _transactions.Select(t => t.GetHash()).ToList();
            await _transactionManager.RemoveTransactionsAsync(transactionIds);
            await RemoveTransactionResultsAsync(transactionIds, _block.GetHash());

            await _transactionPoolService.CleanByTransactionIdsAsync(_transactions.Select(t => t.GetHash()).ToList());
            await _transactionPoolService.UpdateTransactionPoolByBestChainAsync(_chain.BestChainHash,
                _chain.BestChainHeight);
        }
    }
}