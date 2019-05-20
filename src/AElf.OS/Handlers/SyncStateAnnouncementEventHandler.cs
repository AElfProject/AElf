using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class SyncStateAnnouncementEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        private readonly IPeerPool _peerPool;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IBlockchainService _blockchainService;
        
        public ILogger<SyncStateAnnouncementEventHandler> Logger { get; set; }

        public SyncStateAnnouncementEventHandler(IPeerPool peerPool, 
            IBlockchainNodeContextService blockchainNodeContextService,
            IBlockchainService blockchainService)
        {
            _peerPool = peerPool;
            _blockchainNodeContextService = blockchainNodeContextService;
            _blockchainService = blockchainService;
        }
        
        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            await UpdateSyncState(eventData);
        }

        private async Task UpdateSyncState(AnnouncementReceivedEventData eventData)
        {
            if (eventData?.Announce == null)
            {
                Logger?.LogWarning("Null announcement received.");
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

                if (blockHeight > chain.BestChainHeight + NetworkConsts.DefaultMinBlockGapBeforeSync)
                {
                    if (_blockchainNodeContextService.SetSyncing(true))
                        Logger.LogDebug($"Starting a sync phase, best chain height: {chain.BestChainHeight}.");

                }
                else
                {
                    if (_blockchainNodeContextService.SetSyncing(false))
                        Logger.LogDebug($"Finished a sync phase, best chain height: {chain.BestChainHeight}.");
                }
            }
        }
    }
}