using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Application
{
    public interface ISyncStateService
    {
        bool IsSyncing();
        void SetSyncing(long value);
        Task UpdateSyncState();
    }

    public class SyncStateService : ISyncStateService
    {
        private readonly INodeSyncStateProvider _syncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;

        public ILogger<SyncStateService> Logger { get; set; }
        
        public SyncStateService(INodeSyncStateProvider syncStateProvider, IBlockchainService blockchainService, 
            IBlockchainNodeContextService blockchainNodeContextService)
        {
            _syncStateProvider = syncStateProvider;
            _blockchainService = blockchainService;
            _blockchainNodeContextService = blockchainNodeContextService;
        }
        
        public bool IsSyncing() => _syncStateProvider.IsNodeSyncing();
        public void SetSyncing(long value) => _syncStateProvider.SetSyncing(value);
        
        public async Task UpdateSyncState()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            if (chain.BestChainHeight >= _syncStateProvider.SyncTarget)
            {
                // stop sync
                _syncStateProvider.SetSyncing(-1);
                _blockchainNodeContextService.FinishSync();
                
                Logger.LogDebug($"Initial sync finished at {chain.BestChainHeight}.");
            }
        }
    }
}