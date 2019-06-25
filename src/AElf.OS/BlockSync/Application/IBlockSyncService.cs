using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncService
    {
        Task SyncBlockAsync(Hash blockHash, long blockHeight, int batchRequestBlockCount, string suggestedPeerPubKey);

        void SetBlockSyncAnnouncementEnqueueTime(Timestamp timestamp);
    }
}