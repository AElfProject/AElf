using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncValidationService
    {
        Task<bool> ValidateAnnouncementBeforeSyncAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubKey);

        Task<bool> ValidateBlockBeforeSyncAsync(Chain chain, BlockWithTransactions blockWithTransactions, string senderPubKey);

        Task<bool> ValidateBlockBeforeAttachAsync(BlockWithTransactions blockWithTransactions);
    }
}