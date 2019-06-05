using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncService
    {
        Task SyncBlockAsync(Hash blockHash, long blockHeight, int batchRequestBlockCount, string suggestedPeerPubKey);

        Timestamp GetBlockSyncAnnouncementEnqueueTime();

        void SetBlockSyncAnnouncementEnqueueTime(Timestamp timestamp);
        
        Timestamp GetBlockSyncAttachBlockEnqueueTime();
    }

    public class BlockSyncService : IBlockSyncService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockFetchService _blockFetchService;
        private readonly IBlockDownloadService _blockDownloadService;
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public ILogger<BlockSyncService> Logger { get; set; }

        private readonly Duration _blockSyncJobAgeLimit = new Duration {Nanos = 500_000_000};

        public BlockSyncService(IBlockchainService blockchainService,
            IBlockFetchService blockFetchService,
            IBlockDownloadService blockDownloadService,
            IAnnouncementCacheProvider announcementCacheProvider,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockSyncService>.Instance;

            _blockchainService = blockchainService;
            _blockFetchService = blockFetchService;
            _blockDownloadService = blockDownloadService;
            _announcementCacheProvider = announcementCacheProvider;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task SyncBlockAsync(Hash blockHash, long blockHeight, int batchRequestBlockCount,
            string suggestedPeerPubKey)
        {
            if (_blockSyncStateProvider.BlockSyncJobEnqueueTime != null
                && TimestampHelper.GetUtcNow() >
                _blockSyncStateProvider.BlockSyncJobEnqueueTime + _blockSyncJobAgeLimit)
            {
                Logger.LogWarning(
                    $"Queue is too busy, block sync job enqueue timestamp: {_blockSyncStateProvider.BlockSyncJobEnqueueTime.ToDateTime()}");
                return;
            }
            
            if (_announcementCacheProvider.ContainsAnnouncement(blockHash, blockHeight))
            {
                Logger.LogWarning($"The block have been synchronized, block hash: {blockHash}");
                return;
            }
            
            Logger.LogDebug(
                $"Start block sync job, target height: {blockHash}, target block hash: {blockHeight}, peer: {suggestedPeerPubKey}");

            var chain = await _blockchainService.GetChainAsync();
            _announcementCacheProvider.ClearCache(chain.LastIrreversibleBlockHeight);
                        
            bool syncResult;
            if (blockHash != null && blockHeight < chain.BestChainHeight + 8)
            {
                syncResult = await _blockFetchService.FetchBlockAsync(blockHash, blockHeight, suggestedPeerPubKey);
            }
            else
            {
                var syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.BestChainHash,
                    chain.BestChainHeight, batchRequestBlockCount, suggestedPeerPubKey);

                if (syncBlockCount == 0 && blockHeight > chain.LongestChainHeight)
                {
                    Logger.LogDebug($"Resynchronize from lib, lib height: {chain.LastIrreversibleBlockHeight}.");
                    syncBlockCount = await _blockDownloadService.DownloadBlocksAsync(chain.LastIrreversibleBlockHash,
                        chain.LastIrreversibleBlockHeight, batchRequestBlockCount, suggestedPeerPubKey);
                }

                syncResult = syncBlockCount > 0;
            }

            if (syncResult)
            {
                _announcementCacheProvider.CacheAnnouncement(blockHash, blockHeight);
            }

            Logger.LogDebug($"Finishing block sync job, longest chain height: {chain.LongestChainHeight}");
        }
        
        public Timestamp GetBlockSyncAnnouncementEnqueueTime()
        {
            return _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime?.Clone();
        }
        
        public void SetBlockSyncAnnouncementEnqueueTime(Timestamp timestamp)
        {
            _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime = timestamp;
        }
        
        public Timestamp GetBlockSyncAttachBlockEnqueueTime()
        {
            return _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime?.Clone();
        }
    }
}