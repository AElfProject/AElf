using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationService : IBlockSyncValidationService
    {
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
        }

        public Task<bool> ValidateAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement)
        {
            if (!_announcementCacheProvider.TryAddAnnouncementCache(blockAnnouncement.BlockHash,
                blockAnnouncement.BlockHeight))
            {
                return Task.FromResult(false);
            }

            if (blockAnnouncement.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning(
                    $"Receive lower header {{ hash: {blockAnnouncement.BlockHash}, height: {blockAnnouncement.BlockHeight} }} ignore.");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<bool> ValidateBlockAsync(Chain chain, BlockWithTransactions blockWithTransactions)
        {
            if (!_announcementCacheProvider.TryAddAnnouncementCache(blockWithTransactions.GetHash(), blockWithTransactions.Height))
            {
                return Task.FromResult(false);
            }

            if (blockWithTransactions.Height <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning($"Receive lower block {blockWithTransactions} ignore.");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}