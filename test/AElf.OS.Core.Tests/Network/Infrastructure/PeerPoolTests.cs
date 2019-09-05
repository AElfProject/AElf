using System;
using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcPeerPoolTests : NetworkInfrastructureTestBase
    {
        private readonly IPeerPool _peerPool;

        public GrpcPeerPoolTests()
        {
            _peerPool = GetRequiredService<IPeerPool>();
        }
        
        [Fact]
        public void GetPeersByHost_ShouldReturnAllPeers_WithSameHost()
        {
            IPAddress commonHost = IPAddress.Parse("12.34.56.67");
            string commonPort = "1900";
            string commonEndpoint = commonHost + ":" + commonPort;
            
            _peerPool.TryAddPeer(CreatePeer(commonEndpoint));
            _peerPool.TryAddPeer(CreatePeer(commonEndpoint));
            _peerPool.TryAddPeer(CreatePeer("12.34.56.64:1900"));
            _peerPool.TryAddPeer(CreatePeer("12.34.56.61:1900"));

            var peersWithSameHost = _peerPool.GetPeersByIpAddress(commonHost);
            peersWithSameHost.Count.ShouldBe(2);
        }            
        
        [Fact]
        public void AddedPeer_IsFindable_ByAddressAndPubkey()
        {
            var peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.FindPeerByEndpoint(peer.RemoteEndpoint).ShouldNotBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
        }

        [Fact]
        public async Task RemovePeerByPublicKey_ShouldNotBeFindable()
        {
            var peer = CreatePeer();
            
            _peerPool.TryAddPeer(peer);
            _peerPool.RemovePeer(peer.Info.Pubkey);
            
            _peerPool.PeerCount.ShouldBe(0);
            _peerPool.FindPeerByEndpoint(peer.RemoteEndpoint).ShouldBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldBeNull();
        }

        [Fact]
        public async Task CannotAddPeerTwice()
        {
            var peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            _peerPool.TryAddPeer(peer).ShouldBeFalse();
            
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.FindPeerByEndpoint(peer.RemoteEndpoint).ShouldNotBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
        }

        [Fact]
        public async Task AddPeer_MultipleTimes_Test()
        {
            var peer = CreatePeer("127.0.0.1:1000");
            _peerPool.TryAddPeer(peer);
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.IsFull().ShouldBeFalse();

            peer = CreatePeer("127.0.0.1:2000");
            _peerPool.TryAddPeer(peer);
            _peerPool.PeerCount.ShouldBe(2);
            _peerPool.IsFull().ShouldBeTrue();
        }
        
        private static IPeer CreatePeer(string ipEndpoint = NetworkTestConstants.FakeIpEndpoint)
        {
            var peerMock = new Mock<IPeer>();
                
            var keyPair = CryptoHelper.GenerateKeyPair();
            var pubkey = keyPair.PublicKey.ToHex();
            
            var peerInfo = new PeerInfo
            {
                Pubkey = pubkey,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                IsInbound = true
            };

            if (!IpEndpointHelper.TryParse(ipEndpoint, out var endpoint))
                throw new Exception($"Endpoint {ipEndpoint} could not be parsed.");

            peerMock.Setup(p => p.RemoteEndpoint).Returns(endpoint);
            peerMock.Setup(p => p.Info).Returns(peerInfo);
            
            return peerMock.Object;
        }
    }
}