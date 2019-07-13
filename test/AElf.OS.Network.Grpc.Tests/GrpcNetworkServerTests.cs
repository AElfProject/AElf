using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcNetworkServerTests : GrpcBasicNetworkTestBase
    {
        private readonly IAElfNetworkServer _networkServer;
        private readonly ILocalEventBus _eventBus;
        private readonly IPeerPool _peerPool;

        private readonly NetworkTestContextHelpers _netTestHelpers;

        public GrpcNetworkServerTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _peerPool = GetRequiredService<IPeerPool>();
            _netTestHelpers = GetRequiredService<NetworkTestContextHelpers>();
        }
        
        private GrpcPeer AddPeerToPool(string ip = GrpcTestConstants.FakeIpEndpoint, 
            string pubkey = GrpcTestConstants.FakePubkey)
        {
            var peer = GrpcTestPeerFactory.CreateBasicPeer(ip, pubkey);
            bool added = _peerPool.TryAddPeer(peer);
            
            Assert.True(added);

            return peer;
        }

        #region Lifecycle

        [Fact]
        public async Task Start_ShouldLaunch_NetInitEvent()
        {
            NetworkInitializationFinishedEvent eventData = null;
            _eventBus.Subscribe<NetworkInitializationFinishedEvent>(ed =>
            {
                eventData = ed;
                return Task.CompletedTask;
            });

            await _networkServer.StartAsync();
            await _networkServer.StopAsync();

            eventData.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task Stop_ShouldLaunch_DisconnectAllPeers()
        {
            await _networkServer.StartAsync();
            var peer = AddPeerToPool();
            peer.IsShutdown.ShouldBeFalse();
            await _networkServer.StopAsync();
            peer.IsShutdown.ShouldBeTrue();
        }
        
        #endregion

        #region Dialing

        [Fact] 
        public async Task DialPeerAsync_HostAlreadyInPool_ShouldReturnFalse()
        {
            var peer = AddPeerToPool();
            var added = await _networkServer.DialPeerAsync(peer.IpAddress);
            
            added.ShouldBeFalse();
        }
        
        [Fact] 
        public async Task DialPeerAsync_DialException_ShouldReturnFalse()
        {
            var added = await _networkServer.DialPeerAsync(GrpcTestConstants.DialExceptionIpEndpoint);
            
            added.ShouldBeFalse();
            _peerPool.PeerCount.ShouldBe(0);
        }
        
        [Fact] 
        public async Task DialPeerAsync_KeyAlreadyInPool_ShouldReturnFalse()
        {
            // two different hosts with the same pubkey.
            AddPeerToPool();
            var added = await _networkServer.DialPeerAsync(GrpcTestConstants.FakeIpEndpoint2);
            
            added.ShouldBeFalse();
            _netTestHelpers.AllPeersWhereCleaned().ShouldBeTrue();
        }
        
        [Fact] 
        public async Task DialPeerAsync_GoodPeer_ShouldBeInPool()
        {
            // two different hosts with the same pubkey.
            var added = await _networkServer.DialPeerAsync(GrpcTestConstants.GoodPeerEndpoint);
            
            added.ShouldBeTrue();
            _peerPool.FindPeerByAddress(GrpcTestConstants.GoodPeerEndpoint).ShouldNotBeNull();
        }
        
        [Fact] 
        public async Task DialPeerAsync_GoodPeer_ShouldLaunchConnectionEvent()
        {
            AnnouncementReceivedEventData eventData = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(e =>
            {
                eventData = e;
                return Task.CompletedTask;
            });
            
            // two different hosts with the same pubkey.
            var added = await _networkServer.DialPeerAsync(GrpcTestConstants.GoodPeerEndpoint);
            
            added.ShouldBeTrue();
            _peerPool.FindPeerByAddress(GrpcTestConstants.GoodPeerEndpoint).ShouldNotBeNull();
            
            eventData.ShouldNotBeNull();
        }
        
        [Fact] 
        public async Task DialPeerAsync_HandshakeNetProblem_ShouldReturnFalse()
        {
            var added = await _networkServer.DialPeerAsync(GrpcTestConstants.HandshakeWithNetExceptionIp);
            
            added.ShouldBeFalse();
            _peerPool.PeerCount.ShouldBe(0);
        }
        
        [Fact] 
        public async Task DialPeerAsync_HandshakeError_ShouldReturnFalse()
        {
            var added = await _networkServer.DialPeerAsync(GrpcTestConstants.BadHandshakeIp);
            
            added.ShouldBeFalse();
            _peerPool.PeerCount.ShouldBe(0);
        }

        #endregion
    }
}