using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;
using Volo.Abp.Threading;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockAttachParallelTests : BenchmarkParallelTestBase
    {
        private IBlockchainStore<Chain> _chains;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private IBlockManager _blockManager;
        private IChainManager _chainManager;
        private IBlockchainService _blockchainService;
        private IBlockAttachService _blockAttachService;
        private ITransactionManager _transactionManager;
        private BenchmarkHelper _benchmarkHelper;

        private Chain _chain;
        private Block _prepareBlock;
        private Block _block;
        
        [Params(1)]
        public int GroupCount;

        public int TransactionCount = 100;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _chains = GetRequiredService<IBlockchainStore<Chain>>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _chainManager = GetRequiredService<IChainManager>();
            _blockManager = GetRequiredService<IBlockManager>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _benchmarkHelper = GetRequiredService<BenchmarkHelper>();

            _chain = await _blockchainService.GetChainAsync();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            AsyncHelper.RunSync(async () =>
            {
                var tokenAmount = TransactionCount / GroupCount;
                var (prepareTransactions, keyPairs) =
                    await _benchmarkHelper.PrepareTokenForParallel(GroupCount, tokenAmount);
                _prepareBlock =
                    _benchmarkHelper.GenerateBlock(_chain.BestChainHash, _chain.BestChainHeight, prepareTransactions);
                await _blockchainService.AddTransactionsAsync(prepareTransactions);
                await _blockchainService.AddBlockAsync(_prepareBlock);
                await _blockAttachService.AttachBlockAsync(_prepareBlock);

                var cancellableTransactions =
                    await _benchmarkHelper.GenerateTransactionsWithoutConflictAsync(keyPairs, tokenAmount);
                var chain = await _blockchainService.GetChainAsync();
                _block = _benchmarkHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
                    cancellableTransactions);
                await _blockchainService.AddTransactionsAsync(cancellableTransactions);
                await _blockchainService.AddBlockAsync(_block);
            });
        }

        [Benchmark]
        public void AttachBlockParallelTest()
        {
            AsyncHelper.RunSync(async () => { await _blockAttachService.AttachBlockAsync(_block); });
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            AsyncHelper.RunSync(async () =>
            {
                await CleanupBlockAsync(_block);
                await CleanupBlockAsync(_prepareBlock);
                await _chains.SetAsync(_chain.Id.ToStorageKey(), _chain);
            });
        }

        private async Task CleanupBlockAsync(Block block)
        {
            await _blockStateSets.RemoveAsync(block.GetHash().ToStorageKey());
            await _transactionManager.RemoveTransactionsAsync(block.Body.TransactionIds);
            await RemoveTransactionResultsAsync(block.Body.TransactionIds, block.GetHash());
            await _chainManager.RemoveChainBlockLinkAsync(block.GetHash());
            await _blockManager.RemoveBlockAsync(block.GetHash());
        }
    }
}