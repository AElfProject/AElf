using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public interface ISyncStateService
    {
        bool IsSyncing { get; }
        long CurrentSyncTarget { get; }
        Task TryFindSyncTargetAsync();
        Task UpdateSyncStateAsync();
    }

    public class SyncStateService : ISyncStateService, ISingletonDependency
    {
        private readonly INodeSyncStateProvider _syncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IPeerPool _peerPool;

        public ILogger<SyncStateService> Logger { get; set; }
        
        public SyncStateService(INodeSyncStateProvider syncStateProvider, IBlockchainService blockchainService, 
            IBlockchainNodeContextService blockchainNodeContextService, IPeerPool peerPool)
        {
            _syncStateProvider = syncStateProvider;
            _blockchainService = blockchainService;
            _blockchainNodeContextService = blockchainNodeContextService;
            _peerPool = peerPool;
        }
        
        public bool IsSyncing => _syncStateProvider.IsNodeSyncing();
        public long CurrentSyncTarget => _syncStateProvider.SyncTarget;
        
        private void SetSyncTarget(long value) => _syncStateProvider.SetSyncTarget(value);
        
        private void SetSyncAsFinished()
        {
            _syncStateProvider.SetSyncTarget(-1);
            _blockchainNodeContextService.FinishSync();
        }

        public async Task UpdateSyncStateAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            
            if (chain.BestChainHeight >= _syncStateProvider.SyncTarget)
            {
                // stop sync
                SetSyncAsFinished();
                Logger.LogDebug($"Initial sync finished at {chain.BestChainHeight}.");
            }
        }

        public async Task TryFindSyncTargetAsync()
        {
            // determine if we need to sync or not, based on the peers LIB.
            var peers = _peerPool.GetPeers().Where(p => p.LastKnowLIBHeight > 0).ToList();
            
            if (peers.Count == 0)
            {
                // no peer has a LIB to sync to, stop the sync.
                SetSyncAsFinished();
                Logger.LogDebug("Finishing sync, no peer has as a LIB.");
            }
            else
            {
                // set the target to the lowest LIB
                
                var chain = await _blockchainService.GetChainAsync();
                var minLib = peers.Min(p => p.LastKnowLIBHeight);
                
                if (chain.LastIrreversibleBlockHeight + NetworkConstants.DefaultInitialSyncOffset < minLib)
                {
                    SetSyncTarget(minLib);
                    Logger.LogDebug($"Set sync target to {minLib}.");
                }
                else
                {
                    SetSyncAsFinished();
                    Logger.LogDebug("Finishing sync, no peer has as a LIB high enough.");
                }
            }
        }
    }
}