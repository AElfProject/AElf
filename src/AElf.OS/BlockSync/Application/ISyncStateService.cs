using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.BlockSync.Application
{
    public interface ISyncStateService
    {
        bool IsSyncing();
        Task UpdateSyncState();
    }

    public class SyncStateService : ISyncStateService
    {
        private readonly INodeSyncStateProvider _nodeSyncStateProvider;
        private readonly IBlockchainService _blockchainService;
        
        public SyncStateService(INodeSyncStateProvider nodeSyncStateProvider, IBlockchainService blockchainService)
        {
            _nodeSyncStateProvider = nodeSyncStateProvider;
            _blockchainService = blockchainService;
        }
        
        public bool IsSyncing() => _nodeSyncStateProvider.IsNodeSyncing();
        public void SetSyncing(long value) => _nodeSyncStateProvider.SetSyncing(value);
        
        public async Task UpdateSyncState()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            if (chain.BestChainHeight >= _nodeSyncStateProvider.SyncTarget)
            {
                // stop sync
            }
        }
    }
}