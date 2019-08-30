using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
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
        
        private GrpcPeer AddPeerToPool(string ip = NetworkTestConstants.FakeIpEndpoint, 
            string pubkey = NetworkTestConstants.FakePubkey)
        {
            var peer = GrpcTestPeerHelpers.CreateBasicPeer(ip, pubkey);
            bool added = _peerPool.TryAddPeer(peer);
            
            Assert.True(added);

            return peer;
        }

        #region Lifecycle

        [Fact]
        public async Task Start_ShouldLaunch_NetInitEvent()
        {
            NetworkInitializedEvent eventData = null;
            _eventBus.Subscribe<NetworkInitializedEvent>(ed =>
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
            await _networkServer.DisconnectAsync(peer);
            await _networkServer.StopAsync();
            peer.IsShutdown.ShouldBeTrue();
        }
        
        #endregion

        #region Dialing

        [Fact] 
        public async Task DialPeerAsync_HostAlreadyInPool_ShouldReturnFalse()
        {
            var peer = AddPeerToPool();
            var added = await _networkServer.ConnectAsync(peer.RemoteEndpoint);
            
            added.ShouldBeFalse();
        }
        
        [Fact] 
        public async Task DialPeerAsync_DialException_ShouldReturnFalse()
        {
            IpEndpointHelper.TryParse(NetworkTestConstants.DialExceptionIpEndpoint, out var endpoint);
            var added = await _networkServer.ConnectAsync(endpoint);
            
            added.ShouldBeFalse();
            _peerPool.PeerCount.ShouldBe(0);
        }
        
        [Fact] 
        public async Task DialPeerAsync_KeyAlreadyInPool_ShouldReturnFalse()
        {
            // two different hosts with the same pubkey.
            AddPeerToPool();
            
            IpEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint2, out var endpoint);
            var added = await _networkServer.ConnectAsync(endpoint);
            
            added.ShouldBeFalse();
            _netTestHelpers.AllPeersWhereCleaned().ShouldBeTrue();
        }
        
        [Fact] 
        public async Task DialPeerAsync_GoodPeer_ShouldBeInPool()
        {
            IpEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);

            // two different hosts with the same pubkey.
            var added = await _networkServer.ConnectAsync(endpoint);
            
            added.ShouldBeTrue();
            _peerPool.FindPeerByEndpoint(endpoint).ShouldNotBeNull();
        }
        
        [Fact] 
        public async Task DialPeerAsync_GoodPeer_ShouldLaunchConnectionEvent()
        {
            PeerConnectedEventData eventData = null;
            _eventBus.Subscribe<PeerConnectedEventData>(e =>
            {
                eventData = e;
                return Task.CompletedTask;
            });
            
            // two different hosts with the same pubkey.
            IpEndpointHelper.TryParse(NetworkTestConstants.GoodPeerEndpoint, out var endpoint);
            var added = await _networkServer.ConnectAsync(endpoint);
            
            added.ShouldBeTrue();
            _peerPool.FindPeerByEndpoint(endpoint).ShouldNotBeNull();
            
            eventData.ShouldNotBeNull();
        }
        
        [Fact] 
        public async Task DialPeerAsync_HandshakeNetProblem_ShouldReturnFalse()
        {
            IpEndpointHelper.TryParse(NetworkTestConstants.HandshakeWithNetExceptionIp, out var endpoint);
            var added = await _networkServer.ConnectAsync(endpoint);
            
            added.ShouldBeFalse();
            _peerPool.PeerCount.ShouldBe(0);
        }
        
        [Fact] 
        public async Task DialPeerAsync_HandshakeError_ShouldReturnFalse()
        {
            IpEndpointHelper.TryParse(NetworkTestConstants.BadHandshakeIp, out var endpoint);
            var added = await _networkServer.ConnectAsync(endpoint);
            
            added.ShouldBeFalse();
            _peerPool.PeerCount.ShouldBe(0);
        }

        #endregion
    }
}