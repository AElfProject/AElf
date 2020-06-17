using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerPoolTests : NetworkInfrastructureTestBase
    {
        private readonly IPeerPool _peerPool;
        private readonly IBlackListedPeerProvider _blackListProvider;

        public PeerPoolTests()
        {
            _peerPool = GetRequiredService<IPeerPool>();
            _blackListProvider = GetRequiredService<IBlackListedPeerProvider>();
        }

        [Fact]
        public void AddBlacklistedPeer_ShouldReturnFalse()
        {
            var host = "12.34.56.67";
            _blackListProvider.AddHostToBlackList(host,NetworkConstants.DefaultPeerRemovalSeconds);
            _peerPool.AddHandshakingPeer(host, "somePubKey").ShouldBeFalse();
        }

        [Fact]
        public void GetPeersByHost_ShouldReturnAllPeers_WithSameHost()
        {
            var commonHost = "12.34.56.67";
            string commonPort = "1900";
            string commonEndpoint = commonHost + ":" + commonPort;
            
            _peerPool.TryAddPeer(CreatePeer(commonEndpoint));
            _peerPool.TryAddPeer(CreatePeer(commonEndpoint));
            _peerPool.TryAddPeer(CreatePeer("12.34.56.64:1900"));
            _peerPool.TryAddPeer(CreatePeer("12.34.56.61:1900"));

            var peersWithSameHost = _peerPool.GetPeersByHost(commonHost);
            peersWithSameHost.Count.ShouldBe(2);
        }

        [Fact]
        public void AddHandshakingPeer_WithSameIp_ShouldAddToList()
        {
            var commonHost = "12.34.56.64";
            var mockPubkeyOne = "pub1";
            var mockPubkeyTwo = "pub2";
            
            _peerPool.AddHandshakingPeer(commonHost, mockPubkeyOne);
            _peerPool.AddHandshakingPeer(commonHost, mockPubkeyTwo);

            _peerPool.GetHandshakingPeers().TryGetValue(commonHost, out var peers);
            peers.Values.Count.ShouldBe(2);
            peers.Values.ShouldContain(mockPubkeyOne, mockPubkeyTwo);
        }

        [Fact]
        public void RemoveHandshakingPeer_Last_ShouldRemoveEndpoint()
        {
            var commonHost = "12.34.56.64";
            var mockPubkeyOne = "pub1";
            var mockPubkeyTwo = "pub2";
            
            _peerPool.AddHandshakingPeer(commonHost, mockPubkeyOne);
            _peerPool.AddHandshakingPeer(commonHost, mockPubkeyTwo);
            
            _peerPool.RemoveHandshakingPeer(commonHost, mockPubkeyOne);
            _peerPool.GetHandshakingPeers().TryGetValue(commonHost, out _).ShouldBeTrue();
            
            // remove last should remove entry
            _peerPool.RemoveHandshakingPeer(commonHost, mockPubkeyTwo);
            _peerPool.GetHandshakingPeers().TryGetValue(commonHost, out _).ShouldBeFalse();

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
        public void ReplacePeer_Test()
        {
            var peer = CreatePeer();
            var otherPeer = CreatePeer(NetworkTestConstants.FakeIpEndpoint, peer.Info.Pubkey);
            
            _peerPool.TryAddPeer(peer);
            
            peer.Equals(otherPeer).ShouldBeFalse();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();

            _peerPool.TryReplace(peer.Info.Pubkey, peer, otherPeer);
            var replaced = _peerPool.FindPeerByPublicKey(peer.Info.Pubkey);
            replaced.ShouldNotBeNull();
            replaced.Equals(otherPeer).ShouldBeTrue();
        }

        [Fact]
        public void RemovePeerByPublicKey_ShouldNotBeFindable()
        {
            var peer = CreatePeer();
            
            _peerPool.TryAddPeer(peer);
            _peerPool.RemovePeer(peer.Info.Pubkey);
            
            _peerPool.PeerCount.ShouldBe(0);
            _peerPool.FindPeerByEndpoint(peer.RemoteEndpoint).ShouldBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldBeNull();
        }

        [Fact]
        public void CannotAddPeerTwice()
        {
            var peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            _peerPool.TryAddPeer(peer).ShouldBeFalse();
            
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.FindPeerByEndpoint(peer.RemoteEndpoint).ShouldNotBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
        }

        [Fact]
        public void AddPeer_MultipleTimes_Test()
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
        
        [Fact]
        public void AddHandshakingPeer_PeerPoolIsFull_ShouldReturnFalse()
        {
            var peer1 = CreatePeer("127.0.0.1:8801");
            _peerPool.TryAddPeer(peer1);
            var peer2 = CreatePeer("127.0.0.1:8802");
            _peerPool.TryAddPeer(peer2);

            var handshakingPeerHost = "192.168.100.100";
            var addResult = _peerPool.AddHandshakingPeer(handshakingPeerHost, "pubkey");
            addResult.ShouldBeFalse();
            _peerPool.GetHandshakingPeers().ShouldNotContainKey(handshakingPeerHost);
        }

        private static IPeer CreatePeer(string ipEndpoint = NetworkTestConstants.FakeIpEndpoint, string pubKey = null)
        {
            var peerMock = new Mock<IPeer>();
            var pubkey = pubKey ?? CryptoHelper.GenerateKeyPair().PublicKey.ToHex();
            
            var peerInfo = new PeerConnectionInfo
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

        private static DnsEndPoint ParseEndPoint(string ipEndpoint)
        {
            if (!AElfPeerEndpointHelper.TryParse(ipEndpoint, out var endpoint))
                throw new Exception($"Endpoint {ipEndpoint} could not be parsed.");

            return endpoint;
        }
    }
}