using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface INodeSyncStateProvider
    {
        bool IsNodeSyncing();
        void SetSyncing(long target);
        long SyncTarget { get; }
    }
    
    public class NodeSyncStateProvider : INodeSyncStateProvider, ISingletonDependency
    {
        /// <summary>
        /// The target block height of the sync. 0 is the initial state, meaning
        /// the target has never being set. The value is set to -1 when the sync
        /// is finished.
        /// </summary>
        public long SyncTarget { get; private set; } = 0;
        
        public bool IsNodeSyncing()
        {
            return SyncTarget != -1;
        }

        public void SetSyncing(long target)
        {
            SyncTarget = target;
        }
    }
}