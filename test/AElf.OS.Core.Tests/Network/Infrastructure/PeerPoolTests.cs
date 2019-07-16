using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
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
        
        // todo move to handshake provider tests
//        [Fact]
//        public async Task GetHandshakeAsync()
//        {
//            var handshake = await _pool.GetHandshakeAsync();
//            handshake.ShouldNotBeNull();
//            handshake.HandshakeData.Version.ShouldBe(KernelConstants.ProtocolVersion);
//        }
            
        [Fact]
        public void AddedPeer_IsFindable_ByAddressAndPubkey()
        {
            var peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldNotBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
        }

        [Fact]
        public async Task RemovePeerByPublicKey_ShouldNotBeFindable()
        {
            var peer = CreatePeer();
            
            _peerPool.TryAddPeer(peer);
            _peerPool.RemovePeer(peer.Info.Pubkey);
            
            _peerPool.PeerCount.ShouldBe(0);
            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldBeNull();
        }

        [Fact]
        public async Task CannotAddPeerTwice()
        {
            var peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            _peerPool.TryAddPeer(peer).ShouldBeFalse();
            
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.FindPeerByAddress(peer.IpAddress).ShouldNotBeNull();
            _peerPool.FindPeerByPublicKey(peer.Info.Pubkey).ShouldNotBeNull();
        }
        
        private static IPeer CreatePeer(string ip = NetworkTestConstants.FakeIpEndpoint)
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

            peerMock.Setup(p => p.IpAddress).Returns(ip);
            peerMock.Setup(p => p.Info).Returns(peerInfo);
            
            return peerMock.Object;
        }
    }
}