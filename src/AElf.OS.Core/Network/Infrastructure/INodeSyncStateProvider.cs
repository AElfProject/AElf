using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface INodeSyncStateProvider
    {
        long SyncTarget { get; }
        bool GetSyncTarget();
        void SetSyncTarget(long target);
    }
    
    public class NodeSyncStateProvider : INodeSyncStateProvider, ISingletonDependency
    {
        /// <summary>
        /// The target block height of the sync. 0 is the initial state, meaning
        /// the target has never being set. The value is set to -1 when the sync
        /// is finished.
        /// </summary>
        public long SyncTarget { get; private set; } = 0;
        
        public bool GetSyncTarget()
        {
            return SyncTarget != -1;
        }

        public void SetSyncTarget(long target)
        {
            SyncTarget = target;
        }
    }
}