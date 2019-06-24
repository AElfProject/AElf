using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcPeerPoolTests : GrpcNetworkTestBase
    {
        private const string TestIp = "127.0.0.1:6800";
        private readonly string _testPubKey;

        private readonly GrpcPeerInfo _peerInfo;

        private readonly GrpcPeerPool _pool;

        public GrpcPeerPoolTests()
        {
            _pool = GetRequiredService<GrpcPeerPool>();

            var keyPair = CryptoHelpers.GenerateKeyPair();
            _testPubKey = keyPair.PublicKey.ToHex();
            
            _peerInfo = new GrpcPeerInfo
            {
                PublicKey = _testPubKey,
                PeerIpAddress = TestIp,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = 1,
                IsInbound = true
            };
        }
            
        [Fact]
        public void GetPeer_RemoteAddressOrPubKeyAlreadyPresent_ShouldReturnPeer()
        {
            _pool.AddPeer(new GrpcPeer(null, null, _peerInfo));
            
            Assert.NotNull(_pool.FindPeerByAddress(TestIp));
            Assert.NotNull(_pool.FindPeerByPublicKey(_testPubKey));
        }

        [Fact]
        public async Task AddPeerAsync_PeerAlreadyConnected_ShouldReturnFalse()
        {
            _pool.AddPeer( new GrpcPeer(null, null, _peerInfo));

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
            var result = _pool.FindPeerByAddress(GrpcTestConstants.FakePubKey);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetHandshakeAsync()
        {
            var handshake = await _pool.GetHandshakeAsync();
            handshake.ShouldNotBeNull();
            handshake.HandshakeData.Version.ShouldBe(KernelConstants.ProtocolVersion);
        }

        [Fact]
        public async Task RemovePeerByAddress()
        {
            var channel = new Channel(TestIp, ChannelCredentials.Insecure);
            var client = new PeerService.PeerServiceClient(channel);
            _pool.AddPeer(new GrpcPeer(channel, client, _peerInfo));
            _pool.FindPeerByAddress(TestIp).ShouldNotBeNull();

            await _pool.RemovePeerByAddressAsync(TestIp);
            _pool.FindPeerByAddress(TestIp).ShouldBeNull();
        }
    }
}