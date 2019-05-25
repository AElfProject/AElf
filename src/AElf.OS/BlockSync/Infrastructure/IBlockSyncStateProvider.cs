using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockSyncStateProvider
    {
        Timestamp BlockSyncJobEnqueueTime { get; set; }
        
        Timestamp BlockSyncAnnouncementEnqueueTime { get; set; }
    }

    public class BlockSyncStateProvider : IBlockSyncStateProvider, ISingletonDependency
    {
        public Timestamp BlockSyncJobEnqueueTime { get; set; }
        
        public Timestamp BlockSyncAnnouncementEnqueueTime { get; set; }
    }
}