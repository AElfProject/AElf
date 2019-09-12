using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PeerPoolTests : NetworkInfrastructureTestBase
    {
        private readonly IPeerPool _peerPool;

        public PeerPoolTests()
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
        public void AddHandshakingPeer_WithSameIp_ShouldAddToList()
        {
            var commonIp = IPAddress.Parse("12.34.56.64");
            var mockPubkeyOne = "pub1";
            var mockPubkeyTwo = "pub2";
            
            _peerPool.AddHandshakingPeer(commonIp, mockPubkeyOne);
            _peerPool.AddHandshakingPeer(commonIp, mockPubkeyTwo);

            _peerPool.GetHandshakingPeers().TryGetValue(commonIp, out var peers);
            peers.Values.Count.ShouldBe(2);
            peers.Values.ShouldContain(mockPubkeyOne, mockPubkeyTwo);
        }
        
        [Fact]
        public void RemoveHandshakingPeer_Last_ShouldRemoveEndpoint()
        {
            var commonIp = IPAddress.Parse("12.34.56.64");
            var mockPubkeyOne = "pub1";
            var mockPubkeyTwo = "pub2";
            
            _peerPool.AddHandshakingPeer(commonIp, mockPubkeyOne);
            _peerPool.AddHandshakingPeer(commonIp, mockPubkeyTwo);
            
            _peerPool.RemoveHandshakingPeer(commonIp, mockPubkeyOne);
            _peerPool.GetHandshakingPeers().TryGetValue(commonIp, out _).ShouldBeTrue();
            
            // remove last should remove entry
            _peerPool.RemoveHandshakingPeer(commonIp, mockPubkeyTwo);
            _peerPool.GetHandshakingPeers().TryGetValue(commonIp, out _).ShouldBeFalse();

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
                ConnectionTime = TimestampHelper.GetUtcNow(),
                IsInbound = true
            };

            var endpoint = ParseEndPoint(ipEndpoint);

            peerMock.Setup(p => p.RemoteEndpoint).Returns(endpoint);
            peerMock.Setup(p => p.Info).Returns(peerInfo);
            
            return peerMock.Object;
        }

        private static IPEndPoint ParseEndPoint(string ipEndpoint)
        {
            if (!IpEndPointHelper.TryParse(ipEndpoint, out var endpoint))
                throw new Exception($"Endpoint {ipEndpoint} could not be parsed.");

            return endpoint;
        }
    }
}