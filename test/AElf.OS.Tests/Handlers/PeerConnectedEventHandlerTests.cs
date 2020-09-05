using System.Threading.Tasks;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandlerTests : OSTestBase
    {
        private readonly PeerConnectedEventHandler _peerConnectedEventHandler;
        private readonly ILocalEventBus _eventBus;
        private readonly IPeerDiscoveryService _peerDiscoveryService;

        public PeerConnectedEventHandlerTests()
        {
            _peerConnectedEventHandler = GetRequiredService<PeerConnectedEventHandler>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _peerDiscoveryService = GetRequiredService<IPeerDiscoveryService>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var eventData = new PeerConnectedEventData
            (
                new NodeInfo{Endpoint = "127.0.0.1:1234",Pubkey = ByteString.CopyFromUtf8("Pubkey")},
                HashHelper.ComputeFrom("BestChainHash"),
                100
            );

            AnnouncementReceivedEventData announcementEventData = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(d =>
            {
                announcementEventData = d;
                return Task.CompletedTask;
            });

            await _peerConnectedEventHandler.HandleEventAsync(eventData);

            var nodes = await _peerDiscoveryService.GetNodesAsync(1);
            nodes.Nodes[0].ShouldBe(eventData.NodeInfo);
            
            announcementEventData.ShouldNotBeNull();
            announcementEventData.Announce.BlockHash.ShouldBe(eventData.BestChainHash);
            announcementEventData.Announce.BlockHeight.ShouldBe(eventData.BestChainHeight);
            announcementEventData.SenderPubKey.ShouldBe(eventData.NodeInfo.Pubkey.ToHex());
        }
    }
}