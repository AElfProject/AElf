using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface INodeSyncStateProvider
    {
        bool IsNodeSyncing();
        void SetSyncing(long target);
        long SyncTarget { get; }
    }
    
    public class NodeSyncStateProvider : INodeSyncStateProvider, ISingletonDependency
    {
        public long SyncTarget { get; private set; }

        public bool IsNodeSyncing()
        {
            return SyncTarget != 0;
        }

        public void SetSyncing(long target)
        {
            SyncTarget = target;
        }
    }
}