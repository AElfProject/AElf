using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;

namespace AElf.OS.BlockSync.Application
{
    public interface IAnnouncementSyncService
    {
        Task SyncByAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubkey,
            int networkOptionsBlockIdRequestCount);
    }
}