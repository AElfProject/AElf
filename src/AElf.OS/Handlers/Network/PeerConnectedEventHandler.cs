using System.Threading.Tasks;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>
    {
        public ILogger<AnnouncementReceivedEventHandler> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }

        private readonly IPeerDiscoveryService _peerDiscoveryService;

        public PeerConnectedEventHandler(IPeerDiscoveryService peerDiscoveryService)
        {
            _peerDiscoveryService = peerDiscoveryService;
        }

        public async Task HandleEventAsync(PeerConnectedEventData eventData)
        {
            Logger.LogDebug($"Peer connection event {eventData.NodeInfo}");
            
            await _peerDiscoveryService.AddNodeAsync(eventData.NodeInfo);
            
            var blockAnnouncement = new BlockAnnouncement {
                BlockHash = eventData.BestChainHead.GetHash(),
                BlockHeight = eventData.BestChainHead.Height
            };
            
            var announcement = new AnnouncementReceivedEventData(blockAnnouncement, eventData.NodeInfo.Pubkey.ToHex());
            
            await LocalEventBus.PublishAsync(announcement);
        }
    }
}