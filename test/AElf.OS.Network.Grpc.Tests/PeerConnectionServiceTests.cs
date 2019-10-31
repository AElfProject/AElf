using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerConnectionServiceTests : ServerServiceTestBase
    {
        private readonly IConnectionService _connectionService;
        private readonly IPeerPool _peerPool;

        public PeerConnectionServiceTests()
        {
            _connectionService = GetRequiredService<IConnectionService>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task DoHandshake_MaxPeersPerHostReached_Test()
        {
            var peerEndpoint = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234);

            var firstPeerFromHostResp = await _connectionService.DoHandshakeAsync(peerEndpoint, GetHandshake("pubKey1"));
            firstPeerFromHostResp.Error.ShouldBe(HandshakeError.HandshakeOk);
            
            var secondPeerFromHostResp = await _connectionService.DoHandshakeAsync(peerEndpoint, GetHandshake("pubKey2"));
            secondPeerFromHostResp.Error.ShouldBe(HandshakeError.ConnectionRefused);
            
            _peerPool.GetPeers(true).Count.ShouldBe(1);
            _peerPool.GetHandshakingPeers().Values.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task DoHandshake_LocalhostMaxHostIgnored_Test()
        {
            var grpcUriLocal = new IPEndPoint(IPAddress.Loopback, 1234);
            var grpcUriLocalhostIp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);

            var firstLocalPeerResp = await _connectionService.DoHandshakeAsync(grpcUriLocal, GetHandshake("pubKey1"));
            firstLocalPeerResp.Error.ShouldBe(HandshakeError.HandshakeOk);
            
            var secondLocalPeerResp = await _connectionService.DoHandshakeAsync(grpcUriLocal, GetHandshake("pubKey2"));
            secondLocalPeerResp.Error.ShouldBe(HandshakeError.HandshakeOk);
            
            var thirdPeerFromHostResp = await _connectionService.DoHandshakeAsync(grpcUriLocalhostIp, GetHandshake("pubKey3"));
            thirdPeerFromHostResp.Error.ShouldBe(HandshakeError.HandshakeOk);
            
            _peerPool.GetPeers(true).Count.ShouldBe(3);
            _peerPool.GetHandshakingPeers().Values.Count.ShouldBe(0);
        }

        private Handshake GetHandshake(string pubKey = "mockPubKey")
        {
            return new Handshake { HandshakeData = new HandshakeData { Pubkey = ByteString.CopyFromUtf8(pubKey)}};
        }
    }
}