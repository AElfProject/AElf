using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationService : IBlockSyncValidationService
    {
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        private readonly IBlockSyncQueueService _blockSyncQueueService;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider,
            IBlockSyncQueueService blockSyncQueueService)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
            _blockSyncQueueService = blockSyncQueueService;
        }

        public async Task<bool> ValidateBeforeHandleAnnounceAsync(Chain chain, Hash syncBlockHash, long syncBlockHeight)
        {
            if(!_blockSyncQueueService.IsQueueAvailable(OSConstants.BlockFetchQueueName))
            {
                Logger.LogWarning($"Block sync fetch queue is too busy.");
                return false;
            }
            
            if(!_blockSyncQueueService.IsQueueAvailable(OSConstants.BlockDownloadQueueName))
            {
                Logger.LogWarning(
                    $"Block sync download queue is too busy.");
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

        public async Task<bool> ValidateBeforeHandleBlockAsync(Chain chain, BlockWithTransactions blockWithTransactions)
        {
            if(!_blockSyncQueueService.IsQueueAvailable(OSConstants.BlockFetchQueueName))
            {
                Logger.LogWarning("Block sync fetch queue is too busy.");
                return false;
            }
            
            if (!_announcementCacheProvider.TryAddAnnouncementCache(blockWithTransactions.GetHash(), blockWithTransactions.Height))
            {
                return false;
            }

            if (blockWithTransactions.Height <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning($"Receive lower block {blockWithTransactions} ignore.");
                return false;
            }

            return true;
        }
    }
}