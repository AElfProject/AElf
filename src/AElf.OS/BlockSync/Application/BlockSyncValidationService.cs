using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationService : IBlockSyncValidationService
    {
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly IBlockSyncStateProvider _blockSyncStateProvider;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockSyncStateProvider = blockSyncStateProvider;
        }

        public async Task<bool> ValidateBeforeSync(Chain chain, Hash syncBlockHash, long syncBlockHeight)
        {
            var blockFetchEnqueueTime = _blockSyncStateProvider.BlockSyncFetchBlockEnqueueTime;
            if (blockFetchEnqueueTime != null && TimestampHelper.GetUtcNow() > blockFetchEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncFetchBlockAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync fetch queue is too busy, enqueue timestamp: {blockFetchEnqueueTime}");
                return false;
            }
            
            var blockDownloadEnqueueTime = _blockSyncStateProvider.BlockSyncDownloadBlockEnqueueTime;
            if (blockDownloadEnqueueTime != null && TimestampHelper.GetUtcNow() > blockDownloadEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncDownloadBlockAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync download queue is too busy, enqueue timestamp: {blockDownloadEnqueueTime}");
                return false;
            }

            if (!_announcementCacheProvider.TryAddAnnouncementCache(syncBlockHash, syncBlockHeight))
            {
                return false;
            }

            if (syncBlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning($"Receive lower header {{ hash: {syncBlockHash}, height: {syncBlockHeight} }} ignore.");
                return false;
            }

            return true;
        }
    }
}