using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Node.Infrastructure
{
    public class NodeSyncStateProvider : INodeSyncStateProvider, ISingletonDependency
    {
        private volatile bool _isSyncing = false;
        
        public bool IsNodeSyncing() => _isSyncing;

        public bool SetSyncing(bool value)
        {
            if (_isSyncing == value)
                return false;

            _isSyncing = value;

            return true;
        }
    }
}