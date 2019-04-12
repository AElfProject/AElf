using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.OS.Network.Grpc;
using Grpc.Core;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    // todo more unit tests can be done here 
    public class GrpcPeerPoolTests : GrpcNetworkTestBase
    {
        private const string TestIp = "127.0.0.1:6800";
        private readonly string _testPubKey;

        private readonly GrpcPeerPool _pool;

        public GrpcPeerPoolTests()
        {
            _pool = GetRequiredService<GrpcPeerPool>();

            var keyPair = CryptoHelpers.GenerateKeyPair();
            _testPubKey = keyPair.PublicKey.ToHex();
        }
            
        [Fact]
        public void GetPeer_RemoteAddressOrPubKeyAlreadyPresent_ShouldReturnPeer()
        {
            _pool.AddPeer(new GrpcPeer(null, null, _testPubKey, TestIp));
            
            Assert.NotNull(_pool.FindPeerByAddress(TestIp));
            Assert.NotNull(_pool.FindPeerByPublicKey(_testPubKey));
        }

        [Fact]
        public async Task AddPeerAsync_PeerAlreadyConnected_ShouldReturnFalse()
        {
            _pool.AddPeer(new GrpcPeer(null, null, _testPubKey, TestIp));

            var added = await _pool.AddPeerAsync(TestIp);
            
            Assert.False(added);
        }
        
        [Fact]
        public async Task AddPeerAsync_Connect_NotExistPeer_ShouldReturnFalse()
        {
            var testIp = "127.0.0.1:6810";
            var added = await _pool.AddPeerAsync(testIp);
            
            Assert.False(added);
        }

        [Fact]
        public void IsAuthenticatePeer_Success()
        {
            var result = _pool.IsAuthenticatePeer(GrpcTestConstants.FakePubKey);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task GetHandshakeAsync()
        {
            var handshake = await _pool.GetHandshakeAsync();
            handshake.ShouldNotBeNull();
            handshake.HskData.Version.ShouldBe(KernelConstants.ProtocolVersion);
        }

        [Fact]
        public async Task RemovePeerByAddress()
        {
            var channel = new Channel(TestIp, ChannelCredentials.Insecure);
            var client = new PeerService.PeerServiceClient(channel);
            _pool.AddPeer(new GrpcPeer(channel, client, _testPubKey, TestIp));
            _pool.FindPeerByAddress(TestIp).ShouldNotBeNull();

            await _pool.RemovePeerByAddressAsync(TestIp);
            _pool.FindPeerByAddress(TestIp).ShouldBeNull();
        }
    }
}