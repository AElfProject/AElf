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
        private readonly IBlockchainService _blockchainService;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        public BlockSyncValidationService(IBlockchainService blockchainService,
            IAnnouncementCacheProvider announcementCacheProvider,
            IBlockSyncStateProvider blockSyncStateProvider)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockSyncStateProvider = blockSyncStateProvider;
            _blockchainService = blockchainService;
        }

        public async Task<bool> ValidateBeforeEnqueue(Hash syncBlockHash, long syncBlockHeight)
        {
            var announcementEnqueueTime = _blockSyncStateProvider.BlockSyncAnnouncementEnqueueTime;
            if (announcementEnqueueTime != null && TimestampHelper.GetUtcNow() > announcementEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncAnnouncementAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync announcement queue is too busy, enqueue timestamp: {announcementEnqueueTime.ToDateTime()}");
                return false;
            }

            var blockSyncAttachBlockEnqueueTime = _blockSyncStateProvider.BlockSyncAttachBlockEnqueueTime;
            if (blockSyncAttachBlockEnqueueTime != null && TimestampHelper.GetUtcNow() >
                blockSyncAttachBlockEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncAttachBlockAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync attach queue is too busy, enqueue timestamp: {blockSyncAttachBlockEnqueueTime.ToDateTime()}");
                return false;
            }

            var blockSyncAttachAndExecuteBlockEnqueueTime =
                _blockSyncStateProvider.BlockSyncAttachAndExecuteBlockJobEnqueueTime;
            if (blockSyncAttachAndExecuteBlockEnqueueTime != null && TimestampHelper.GetUtcNow() >
                blockSyncAttachAndExecuteBlockEnqueueTime +
                TimestampHelper.DurationFromMilliseconds(BlockSyncConstants.BlockSyncAttachAndExecuteBlockAgeLimit))
            {
                Logger.LogWarning(
                    $"Block sync attach and execute queue is too busy, enqueue timestamp: {blockSyncAttachAndExecuteBlockEnqueueTime.ToDateTime()}");
                return false;
            }

            if (!_announcementCacheProvider.AddAnnouncementCache(syncBlockHash, syncBlockHeight))
            {
                return false;
            }

            var chain = await _blockchainService.GetChainAsync();
            if (syncBlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogTrace($"Receive lower header {{ hash: {syncBlockHash}, height: {syncBlockHeight} }} ignore.");
                return false;
            }

            return true;
        }
    }
}