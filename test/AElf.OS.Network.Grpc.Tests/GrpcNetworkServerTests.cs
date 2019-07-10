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

        public GrpcNetworkServerTests()
        {
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _peerPool = GetRequiredService<IPeerPool>();
        }
        
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
        public async Task AddPeerAsync_PeerAlreadyConnected_ShouldReturnFalse()
        {
            AddPeerToPool();
            bool dialed = await _networkServer.DialPeerAsync(string.Empty);
            Assert.False(dialed);
        }

        private GrpcPeer AddPeerToPool(string ip = GrpcTestConstants.FakeIpEndpoint, 
            string pubkey = GrpcTestConstants.FakePubkey)
        {
            var peer = GrpcTestHelper.CreateBasicPeer(ip, pubkey);
            bool added = _peerPool.TryAddPeer(peer);
            
            Assert.True(added);

            return peer;
        }
        
        [Fact]
        public async Task AddPeerAsync_Connect_NotExistPeer_ShouldReturnFalse()
        {
            var testIp = "127.0.0.1:6810";
            var added = await _peerPool.AddPeerAsync(testIp);
            
            Assert.False(added);
        }
    }
}