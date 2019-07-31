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

        public ILogger<BlockSyncValidationService> Logger { get; set; }

        public BlockSyncValidationService(IAnnouncementCacheProvider announcementCacheProvider)
        {
            Logger = NullLogger<BlockSyncValidationService>.Instance;

            _announcementCacheProvider = announcementCacheProvider;
        }

        public async Task<bool> ValidateAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubKey)
        {
            if (!TryCacheNewAnnouncement(blockAnnouncement.BlockHash, blockAnnouncement.BlockHeight, senderPubKey))
            {
                return false;
            }

            if (blockAnnouncement.BlockHeight <= chain.LastIrreversibleBlockHeight)
            {
                Logger.LogWarning(
                    $"Receive lower header {{ hash: {blockAnnouncement.BlockHash}, height: {blockAnnouncement.BlockHeight} }} ignore.");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockAsync(Chain chain, BlockWithTransactions blockWithTransactions, string senderPubKey)
        {
            if (!TryCacheNewAnnouncement(blockWithTransactions.GetHash(), blockWithTransactions.Height, senderPubKey))
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
        
        private bool TryCacheNewAnnouncement(Hash blockHash, long blockHeight, string senderPubkey)
        {
            return _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(blockHash, blockHeight, senderPubkey);
        }
    }
}