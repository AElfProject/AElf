using System.Threading.Tasks;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Newtonsoft.Json;
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
            var grpcPeer = await _peerDialer.DialPeerAsync("127.0.0.1:2000");
            grpcPeer.ShouldNotBeNull();
        }

        [Fact]
        public async Task DialBackPeer_Test()
        {
            var grpcPeer = await _peerDialer.DialBackPeer("127.0.0.1:2000", new ConnectionInfo
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