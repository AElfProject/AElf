using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockSyncStateProvider
    {
        Timestamp BlockSyncAttachAndExecuteBlockJobEnqueueTime { get; set; }
        
        Timestamp BlockSyncAnnouncementEnqueueTime { get; set; }
        
        Timestamp BlockSyncAttachBlockEnqueueTime { get; set; }
    }
}