using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.OS.Network.Grpc;
using Xunit;

namespace AElf.OS.Network
{
    // todo more unit tests can be done here 
    public class GrpcPeerPoolTests : GrpcNetworkTestBase
    {
        public GrpcPeerPoolTests()
        {
            _pool = GetRequiredService<GrpcPeerPool>();

            var keyPair = CryptoHelpers.GenerateKeyPair();
            _testPubKey = keyPair.PublicKey.ToHex();
        }

        private const string TestIp = "127.0.0.1:6800";
        private readonly string _testPubKey;

        private readonly GrpcPeerPool _pool;

        [Fact]
        public async Task AddPeerAsync_PeerAlreadyConnected_ShouldReturnFalse()
        {
            _pool.AddPeer(new GrpcPeer(null, null, _testPubKey, TestIp));

            var added = await _pool.AddPeerAsync(TestIp);

            Assert.False(added);
        }

        [Fact]
        public void GetPeer_RemoteAddressOrPubKeyAlreadyPresent_ShouldReturnPeer()
        {
            _pool.AddPeer(new GrpcPeer(null, null, _testPubKey, TestIp));

            Assert.NotNull(_pool.FindPeerByAddress(TestIp));
            Assert.NotNull(_pool.FindPeerByPublicKey(_testPubKey));
        }
    }
}