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
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class MinerTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private IMinerService _minerService;
        private ITransactionResultManager _transactionResultManager;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private ITxHub _txHub;
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
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _txHub = GetRequiredService<ITxHub>();

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
            await _transactionResultManager.RemoveTransactionResultsAsync(transactionIds, _block.GetHash());
            await _transactionResultManager.RemoveTransactionResultsAsync(transactionIds,
                _block.Header.GetDisambiguatingHash());

            await _txHub.CleanTransactionsAsync(_transactions.Select(t => t.GetHash()).ToList());

            await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = _chain.BestChainHash,
                BlockHeight = _chain.BestChainHeight
            });
        }
    }
}