using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Protocol.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class PeerPoolNotLimitMaxPeerTests : NetworkPeerPoolNotLimitMaxPeerTestBase
    {
        private readonly IPeerPool _peerPool;

        public PeerPoolNotLimitMaxPeerTests()
        {
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public void IsFull_Test()
        {
            var peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            _peerPool.PeerCount.ShouldBe(1);
            _peerPool.IsFull().ShouldBeFalse();
            
            peer = CreatePeer();
            _peerPool.TryAddPeer(peer);
            _peerPool.PeerCount.ShouldBe(2);
            _peerPool.IsFull().ShouldBeFalse();
        }
        
        private IPeer CreatePeer()
        {
            var peerMock = new Mock<IPeer>();
            var pubkey = CryptoHelper.GenerateKeyPair().PublicKey.ToHex();

            var peerInfo = new PeerConnectionInfo
            {
                Pubkey = pubkey,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow(),
                IsInbound = true
            };
            peerMock.Setup(p => p.Info).Returns(peerInfo);
            
            return peerMock.Object;
        }
    }
}