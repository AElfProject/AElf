using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class SyncStateAnnouncementEventHandler : ILocalEventHandler<PeerConnectionProcessFinished>
    {
        private readonly IPeerPool _peerPool;
        private readonly ISyncStateService _syncStateService;
        private readonly IBlockchainService _blockchainService;
        
        private readonly NetworkOptions _networkOptions;
        
        public ILogger<SyncStateAnnouncementEventHandler> Logger { get; set; }

        public SyncStateAnnouncementEventHandler(IOptionsSnapshot<NetworkOptions> networkOptions, IPeerPool peerPool, 
            ISyncStateService syncStateService,
            IBlockchainService blockchainService)
        {
            _peerPool = peerPool;
            _syncStateService = syncStateService;
            _blockchainService = blockchainService;

            _networkOptions = networkOptions.Value;
        }
        
        public Task HandleEventAsync(PeerConnectionProcessFinished eventData)
        {
            var peers = _peerPool.GetPeers().Where(p => p.LastKnowLIBHeight > 0).ToList();
            
            // if no peers at all or none have an LIB to sync to, stop the sync.
            if (peers.Count == 0)
            {
                // stop the sync
                
            }
            else
            {
                var minLib = peers.Min(p => p.LastKnowLIBHeight);
                // set the target
            }

            return Task.CompletedTask;
        }
        
        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await UpdateSyncState(eventData);
        }

        private async Task UpdateSyncState(AnnouncementReceivedEventData eventData)
        {
            if (eventData.IsFromConnection)
            {
                Logger?.LogWarning("Ignoring connection announcement.");
                return;
            }
            
            if (eventData?.Announce == null)
            {
                Logger.LogWarning("Null announcement received.");
                return;
            }

            var blockHash = eventData.Announce.BlockHash;
            var blockHeight = eventData.Announce.BlockHeight;
            
            var peers = _peerPool.GetPeers().ToList();
            
            var peersWithAnnounce = peers.Where(p =>
            {
                if (p.RecentBlockHeightAndHashMappings.TryGetValue(blockHeight, out var hash))
                    return blockHash == hash;
                
                return false;
            }).ToList();
            
            var necessaryCount = (int) (peers.Count * 2d / 3);
            if (peersWithAnnounce.Count >= necessaryCount)
            {
                var chain = await _blockchainService.GetChainAsync();

                if (blockHeight > chain.BestChainHeight + _networkOptions.MinBlockGapBeforeSync)
                {
                    if (_syncStateService.SetSyncing(true))
                    {
                        Logger.LogDebug($"Starting a sync phase, best chain height: {chain.BestChainHeight}.");
                        // todo call service
                    }
                }
                else
                {
                    if (_syncStateService.SetSyncing(false))
                    {
                        Logger.LogDebug($"Finished a sync phase, best chain height: {chain.BestChainHeight}.");
                        // todo call service
                    }
                }
            }
        }
    }
}