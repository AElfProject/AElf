using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockSyncStateProvider
    {
        Timestamp BlockSyncAttachAndExecuteBlockJobEnqueueTime { get; set; }
        
        Timestamp BlockSyncAnnouncementEnqueueTime { get; set; }
        
        Timestamp BlockSyncAttachBlockEnqueueTime { get; set; }
    }

    public class BlockSyncStateProvider : IBlockSyncStateProvider, ISingletonDependency
    {
        public Timestamp BlockSyncAttachAndExecuteBlockJobEnqueueTime { get; set; }
        
        public Timestamp BlockSyncAnnouncementEnqueueTime { get; set; }
        
        public Timestamp BlockSyncAttachBlockEnqueueTime { get; set; }
    }
}