using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class ConnectionProcessFinishedEventHandler : ILocalEventHandler<PeerConnectionProcessFinished>
    {
        private readonly IPeerPool _peerPool;
        private readonly ISyncStateService _syncStateService;
        private readonly IBlockchainService _blockchainService;

        public ILogger<ConnectionProcessFinishedEventHandler> Logger { get; set; }

        public ConnectionProcessFinishedEventHandler(IPeerPool peerPool, ISyncStateService syncStateService, 
            IBlockchainService blockchainService)
        {
            _peerPool = peerPool;
            _syncStateService = syncStateService;
            _blockchainService = blockchainService;
        }
        
        public async Task HandleEventAsync(PeerConnectionProcessFinished eventData)
        {
            // determine if we need to sync or not, based on the peers LIB.
            var peers = _peerPool.GetPeers().Where(p => p.LastKnowLIBHeight > 0).ToList();
            
            if (peers.Count == 0)
            {
                // no peer has a LIB to sync to, stop the sync.
                _syncStateService.SetSyncing(-1);
                Logger.LogDebug($"Finishing sync, no peer has as a LIB.");
            }
            else
            {
                // set the target to the lowest LIB
                
                var chain = await _blockchainService.GetChainAsync();
                var minLib = peers.Min(p => p.LastKnowLIBHeight);
                
                if (chain.LastIrreversibleBlockHeight + NetworkConstants.DefaultInitialSyncOffset < minLib)
                {
                    _syncStateService.SetSyncing(minLib);
                    Logger.LogDebug($"Set sync target to {minLib}.");
                }
                else
                {
                    _syncStateService.SetSyncing(-1);
                    Logger.LogDebug("Finishing sync, no peer has as a LIB high enough.");
                }
                
            }
        }
    }
}