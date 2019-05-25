using AElf.Kernel.Node.Infrastructure;

namespace AElf.Kernel.Node.Application
{
    public interface IBlockChainNodeStateService
    {
        bool SetSyncing(bool value);
        bool IsNodeSyncing();
    }

    public class BlockChainNodeStateService : IBlockChainNodeStateService
    {
        private readonly INodeSyncStateProvider _nodeSyncStateProvider;
        
        public BlockChainNodeStateService(INodeSyncStateProvider nodeSyncStateProvider)
        {
            _nodeSyncStateProvider = nodeSyncStateProvider;
        }
        
        public bool IsNodeSyncing() => _nodeSyncStateProvider.IsNodeSyncing();
        public bool SetSyncing(bool value) => _nodeSyncStateProvider.SetSyncing(value);
    }
}