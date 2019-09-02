using System.Threading.Tasks;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerDialerTests : GrpcNetworkDialerTestBase
    {
        private readonly IPeerDialer _peerDialer;

        public PeerDialerTests()
        {
            _peerDialer = GetRequiredService<IPeerDialer>();
        }

        [Fact]
        public async Task DialPeer_NotExist_Test()
        {
            var endpoint = IpEndpointHelper.Parse("127.0.0.1:2000");
            var grpcPeer = await _peerDialer.DialPeerAsync(endpoint);
            grpcPeer.ShouldNotBeNull();
        }

        [Fact]
        public async Task DialBackPeer_Test()
        {
            var endpoint = IpEndpointHelper.Parse("127.0.0.1:2000");
            var grpcPeer = await _peerDialer.DialBackPeer(endpoint, new ConnectionInfo
            {
                ChainId = 1,
                ListeningPort = 2000,
                Pubkey = ByteString.CopyFromUtf8("pub-key"),
                Version = 1
            });
            grpcPeer.ShouldNotBeNull();
        }
    }
}