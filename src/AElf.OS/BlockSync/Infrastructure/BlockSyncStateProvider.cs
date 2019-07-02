using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class BlockSyncStateProvider : IBlockSyncStateProvider, ISingletonDependency
    {
        public Timestamp BlockSyncFetchBlockEnqueueTime { get; set; }
        
        public Timestamp BlockSyncDownloadBlockEnqueueTime { get; set; }
        
        public Timestamp BlockSyncAttachAndExecuteBlockJobEnqueueTime { get; set; }
        
        public Timestamp BlockSyncAttachBlockEnqueueTime { get; set; }
    }
}