using AElf.OS.Network.Helpers;
using AElf.OS.Network.Protocol.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class PeerPoolMaxPeersPerIpAddressTests : NetworkMaxPeersPerIpAddressTestBase
    {
        private readonly IPeerPool _peerPool;

        public PeerPoolMaxPeersPerIpAddressTests()
        {
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public void AddHandshakingPeer_LoopbackAddress_Test()
        {
            var handshakingPeerHost = "127.0.0.1";

            for (int i = 0; i < 3; i++)
            {
                var pubkey = "pubkey" + i;
                var addResult = _peerPool.AddHandshakingPeer(handshakingPeerHost, pubkey);
                addResult.ShouldBeTrue();

                var handshakingPeers = _peerPool.GetHandshakingPeers();
                handshakingPeers.ShouldContainKey(handshakingPeerHost);
                handshakingPeers[handshakingPeerHost].ShouldContainKey(pubkey);
            }
        }

        [Fact]
        public void AddHandshakingPeer_OverIpLimit_Test()
        {
            var handshakingPeerHost = "192.168.100.1";

            for (int i = 0; i < 2; i++)
            {
                var pubkey = "pubkey" + i;
                var addResult = _peerPool.AddHandshakingPeer(handshakingPeerHost, pubkey);
                addResult.ShouldBeTrue();

                var handshakingPeers = _peerPool.GetHandshakingPeers();
                handshakingPeers.ShouldContainKey(handshakingPeerHost);
                handshakingPeers[handshakingPeerHost].ShouldContainKey(pubkey);
            }

            {
                var pubkey = "pubkey2";
                var addResult = _peerPool.AddHandshakingPeer(handshakingPeerHost, pubkey);
                addResult.ShouldBeFalse();

                var handshakingPeers = _peerPool.GetHandshakingPeers();
                handshakingPeers[handshakingPeerHost].ShouldNotContainKey(pubkey);
            }
        }

        [Fact]
        public void AddHandshakingPeer_AlreadyInPeerPool_Test()
        {
            var handshakingPeerHost = "192.168.100.1";

            var peerMock = new Mock<IPeer>();
            var peerInfo = new PeerConnectionInfo
            {
                Pubkey = "PeerPubkey",
            };
            AElfPeerEndpointHelper.TryParse("192.168.100.1:8001", out var endpoint);
            peerMock.Setup(p => p.RemoteEndpoint).Returns(endpoint);
            peerMock.Setup(p => p.Info).Returns(peerInfo);
            _peerPool.TryAddPeer(peerMock.Object);

            {
                var pubkey = "pubkey1";
                var addResult = _peerPool.AddHandshakingPeer(handshakingPeerHost, pubkey);
                addResult.ShouldBeTrue();

                var handshakingPeers = _peerPool.GetHandshakingPeers();
                handshakingPeers.ShouldContainKey(handshakingPeerHost);
                handshakingPeers[handshakingPeerHost].ShouldContainKey(pubkey);
            }
            {
                var pubkey = "pubkey2";
                var addResult = _peerPool.AddHandshakingPeer(handshakingPeerHost, pubkey);
                addResult.ShouldBeFalse();

                var handshakingPeers = _peerPool.GetHandshakingPeers();
                handshakingPeers[handshakingPeerHost].ShouldNotContainKey(pubkey);
            }
        }
    }
}