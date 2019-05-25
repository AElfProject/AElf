using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Node.Infrastructure
{
    public class NodeSyncStateProvider : INodeSyncStateProvider, ISingletonDependency
    {
        private readonly object _syncStateLock = new object();
        private bool _isSyncing = true;

        public bool IsNodeSyncing()
        {
            lock (_syncStateLock)
            {
                return _isSyncing;
            }
        }

        public bool SetSyncing(bool value)
        {
            lock (_syncStateLock)
            {
                if (_isSyncing == value)
                    return false;

                _isSyncing = value;
            }

            return true;
        }
    }
}