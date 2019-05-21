using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncService
    {
        Task SyncBlockAsync(Hash blockHash, long blockHeight, int batchRequestBlockCount, string suggestedPeerPubKey);
    }

    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IBlockDownloadHistoryCacheProvider _blockDownloadHistoryCacheProvider;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public ILogger<BlockSyncService> Logger { get; set; }
        
        private readonly TimeSpan _blockSyncJobAgeLimit = TimeSpan.FromSeconds(0.5);

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IBlockDownloadHistoryCacheProvider blockDownloadHistoryCacheProvider,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _blockDownloadHistoryCacheProvider = blockDownloadHistoryCacheProvider;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task SyncBlockAsync(Hash blockHash, long blockHeight, int batchRequestBlockCount,
            string suggestedPeerPubKey)
        {
            Logger.LogDebug(
                $"Start block sync job, target height: {blockHash}, target block hash: {blockHeight}, peer: {suggestedPeerPubKey}");

            var chain = await _blockchainService.GetChainAsync();
            if (blockHash != null && blockHeight < chain.BestChainHeight + 5)
            {
                await _blockFetchService.FetchBlockAsync(blockHash, blockHeight, suggestedPeerPubKey);
            }
            else
            {
                if (_blockSyncStateProvider.BlockSyncJobEnqueueTime != null
                    && DateTime.UtcNow - _blockSyncStateProvider.BlockSyncJobEnqueueTime.ToDateTime() >
                    _blockSyncJobAgeLimit)
                {
                    Logger.LogWarning(
                        $"Queue is too busy, block sync job enqueue timestamp: {_blockSyncStateProvider.BlockSyncJobEnqueueTime.ToDateTime()}");
                    return;
                }
                
                _blockDownloadHistoryCacheProvider.ClearCache(chain.LastIrreversibleBlockHeight);
                if (!_blockDownloadHistoryCacheProvider.CacheHistory(blockHash, blockHeight))
                {
                    Logger.LogWarning($"The block have been synchronized, block hash: {blockHash}");
                    return;
                }

                var syncFromBestChainBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                    chain.BestChainHeight, batchRequestBlockCount, suggestedPeerPubKey);

                if (syncFromBestChainBlockCount == 0 && blockHeight > chain.LongestChainHeight)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    await _blockDownloadService.DownloadBlocksAsync(chain.LastIrreversibleBlockHash,
                        chain.LastIrreversibleBlockHeight, batchRequestBlockCount, suggestedPeerPubKey);
                }
            }

            Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
        }
    }
}