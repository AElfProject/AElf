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
        void SetSyncTarget(long value);
        void SetSyncAsFinished();
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
        public void SetSyncTarget(long value) => _syncStateProvider.SetSyncTarget(value);
        public void SetSyncAsFinished()
        {
            _syncStateProvider.SetSyncTarget(-1);
            _blockchainNodeContextService.FinishSync();
        }

        public async Task UpdateSyncState()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            if (chain.BestChainHeight >= _syncStateProvider.SyncTarget)
            {
                // stop sync
                SetSyncAsFinished();
                Logger.LogDebug($"Initial sync finished at {chain.BestChainHeight}.");
            }
        }
    }
}