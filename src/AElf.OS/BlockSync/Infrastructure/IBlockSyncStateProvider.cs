using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockSyncStateProvider
    {
        Timestamp BlockSyncFetchBlockEnqueueTime { get; set; }
        
        Timestamp BlockSyncDownloadBlockEnqueueTime { get; set; }
        
        Timestamp BlockSyncAttachAndExecuteBlockJobEnqueueTime { get; set; }
        
        Timestamp BlockSyncAttachBlockEnqueueTime { get; set; }
        

    }
}