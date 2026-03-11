using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.BlockPruning.Domain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.BlockPruning.Application;

public class BlockPruningService : IBlockPruningService, ITransientDependency
{
    private readonly IBlockManager _blockManager;
    private readonly IBlockchainService _blockchainService;
    private readonly IChainManager _chainManager;
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly BlockPruningOptions _options;
    private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;
    private readonly ITransactionManager _transactionManager;
    private readonly ITransactionResultManager _transactionResultManager;

    public ILogger<BlockPruningService> Logger { get; set; }

    public BlockPruningService(IBlockchainService blockchainService,
        IChainManager chainManager,
        IBlockManager blockManager,
        ITransactionManager transactionManager,
        ITransactionResultManager transactionResultManager,
        ITransactionBlockIndexManager transactionBlockIndexManager,
        IBlockPruningInfoManager blockPruningInfoManager,
        IOptionsSnapshot<BlockPruningOptions> options)
    {
        _blockchainService = blockchainService;
        _chainManager = chainManager;
        _blockManager = blockManager;
        _transactionManager = transactionManager;
        _transactionResultManager = transactionResultManager;
        _transactionBlockIndexManager = transactionBlockIndexManager;
        _blockPruningInfoManager = blockPruningInfoManager;
        _options = options.Value;

        Logger = NullLogger<BlockPruningService>.Instance;
    }

    public async Task PruneBlockchainDataAsync()
    {
        if (!_options.Enabled)
            return;

        var chain = await _blockchainService.GetChainAsync();
        var pruneTargetHeight = chain.LastIrreversibleBlockHeight - _options.RetainDistance;
        var lastPrunedHeight = await _blockPruningInfoManager.GetLastPrunedHeightAsync();

        if (pruneTargetHeight <= lastPrunedHeight)
            return;

        var gap = pruneTargetHeight - lastPrunedHeight;
        if (gap < _options.PruneThreshold)
        {
            Logger.LogDebug(
                "Pruning skipped: gap {Gap} below threshold {Threshold} (target={Target}, lastPruned={LastPruned})",
                gap, _options.PruneThreshold, pruneTargetHeight, lastPrunedHeight);
            return;
        }

        var startHeight = Math.Max(2, lastPrunedHeight + 1);
        if (startHeight > pruneTargetHeight)
            return;

        Logger.LogInformation(
            "Block pruning started: from height {StartHeight} to {TargetHeight} (LIB={LIBHeight}, retain={RetainDistance})",
            startHeight, pruneTargetHeight, chain.LastIrreversibleBlockHeight, _options.RetainDistance);

        var totalPruned = 0L;

        for (var batchStart = startHeight; batchStart <= pruneTargetHeight; batchStart += _options.BatchSize)
        {
            var batchEnd = Math.Min(batchStart + _options.BatchSize - 1, pruneTargetHeight);

            var allTxIds = new List<Hash>();
            var allTxResultBlockHashes = new List<Hash>();
            var allBlockHashes = new List<Hash>();

            for (var height = batchStart; height <= batchEnd; height++)
            {
                var chainBlockIndex = await _chainManager.GetChainBlockIndexAsync(height);
                if (chainBlockIndex == null)
                    continue;

                var blockHash = chainBlockIndex.BlockHash;
                allBlockHashes.Add(blockHash);

                var block = await _blockManager.GetBlockAsync(blockHash);
                if (block == null)
                    continue;

                foreach (var txId in block.TransactionIds)
                {
                    allTxIds.Add(txId);
                    allTxResultBlockHashes.Add(blockHash);
                }
            }

            await _transactionResultManager.RemoveTransactionResultsAsync(allTxIds, allTxResultBlockHashes);
            await _transactionBlockIndexManager.RemoveTransactionIndicesAsync(allTxIds);
            await _transactionManager.RemoveTransactionsAsync(allTxIds);
            await _blockManager.RemoveBlocksAsync(allBlockHashes);
            await _chainManager.RemoveChainBlockLinksAsync(allBlockHashes);

            await _blockPruningInfoManager.SetLastPrunedHeightAsync(batchEnd);

            totalPruned += batchEnd - batchStart + 1;

            Logger.LogDebug(
                "Pruned batch [{BatchStart}..{BatchEnd}]: {BlockCount} blocks, {TxCount} transactions",
                batchStart, batchEnd, allBlockHashes.Count, allTxIds.Count);

            if (_options.BatchDelayMilliseconds > 0)
                await Task.Delay(_options.BatchDelayMilliseconds);
        }

        Logger.LogInformation(
            "Block pruning completed: pruned {TotalPruned} heights, new last pruned height = {PrunedHeight}",
            totalPruned, pruneTargetHeight);
    }
}
