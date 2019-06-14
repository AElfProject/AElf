using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Node.Application;
using AElf.OS.BlockSync.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Grpc.Core;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class SyncStateAnnouncementEventHandlerTests //: SyncTestBase 
    {
        private ConnectionProcessFinishedEventHandler _handler;
        
        private IPeerPool _peerPool;
        private ISyncStateService _syncStateService;

        public SyncStateAnnouncementEventHandlerTests()
        {
            _handler = GetRequiredService<ConnectionProcessFinishedEventHandler>();
            
            _peerPool = GetRequiredService<IPeerPool>();
            _syncStateService = GetRequiredService<ISyncStateService>();
        }

        [Fact]
        public async Task NullAnnounceShouldNotChangeState()
        {
            _syncStateService.SetSyncTarget(false);
            
            await _handler.HandleEventAsync(new AnnouncementReceivedEventData(null, null));
            
            var isSyncing = _syncStateService.IsSyncing();
            isSyncing.ShouldBeFalse();
        }

        [Fact]
        public async Task OnePeer_InGap_ShouldTriggerSync()
        {
            var announce = new PeerNewBlockAnnouncement { BlockHash = Hash.FromString("block1"), BlockHeight = 15 };

            var bp1 = CreatePeer("bp1");
            bp1.HandlerRemoteAnnounce(announce);
            
            _peerPool.AddPeer(bp1);
            
            // announce from b1 for block1
            var blockAnnounce = new AnnouncementReceivedEventData(announce, "b1");
            await _handler.HandleEventAsync(blockAnnounce);
            
            _syncStateService.IsSyncing().ShouldBeTrue();
        }
        
        [Fact]
        public async Task EnoughAnnouncements_InGap_ShouldTriggerSync()
        {
            var announce = new PeerNewBlockAnnouncement { BlockHash = Hash.FromString("block1"), BlockHeight = 15 };

            var bp1 = CreatePeer("bp1");
            var bp2 = CreatePeer("bp2");
            var bp3 = CreatePeer("bp3");
            
            _peerPool.AddPeer(bp1);
            _peerPool.AddPeer(bp2);
            _peerPool.AddPeer(bp3);
            
            _syncStateService.SetSyncTarget(false);
            
            // b1 announces the block1
            bp1.HandlerRemoteAnnounce(announce);
            var blockAnnounceBp1 = new AnnouncementReceivedEventData(announce, "b1");
            await _handler.HandleEventAsync(blockAnnounceBp1);
            
            // one is not enough
            _syncStateService.IsSyncing().ShouldBeFalse();
            
            // b2 announces the block1
            bp2.HandlerRemoteAnnounce(announce);
            var blockAnnounceBp2 = new AnnouncementReceivedEventData(announce, "b2");
            await _handler.HandleEventAsync(blockAnnounceBp2);
            
            _syncStateService.IsSyncing().ShouldBeTrue();
        }
        
        [Fact]
        public async Task EnoughAnnouncements_OutSideGap_ShouldFinishSync()
        {
            _syncStateService.SetSyncTarget(true);
            
            var announce = new PeerNewBlockAnnouncement { BlockHash = Hash.FromString("block1"), BlockHeight = 11 };

            var bp1 = CreatePeer("bp1");
            var bp2 = CreatePeer("bp2");
            var bp3 = CreatePeer("bp3");
            
            _peerPool.AddPeer(bp1);
            _peerPool.AddPeer(bp2);
            _peerPool.AddPeer(bp3);
            
            // b1, b2 announces the block1
            bp1.HandlerRemoteAnnounce(announce);
            bp2.HandlerRemoteAnnounce(announce);

            await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announce, "b1"));
            
            _syncStateService.IsSyncing().ShouldBeFalse();
        }

        private GrpcPeer CreatePeer(string publicKey)
        {
            var connectionInfo = new GrpcPeerInfo
            {
                PublicKey = publicKey,
                PeerIpAddress = "127.0.0.1:5000",
                ProtocolVersion = 1,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = 1,
                IsInbound = true
            };
            
            return new GrpcPeer(new Channel("127.0.0.1:5000", ChannelCredentials.Insecure), null, connectionInfo);
        }
    }
}