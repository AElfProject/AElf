using System.Threading.Tasks;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerDialerReplyErrorTests : PeerDialerReplyErrorTestBase
    {
        private readonly IPeerDialer _peerDialer;

        public PeerDialerReplyErrorTests()
        {
            _peerDialer = GetRequiredService<IPeerDialer>();
        }

        [Fact]
        public async Task DialPeer_Test()
        {
            var endpoint = IpEndpointHelper.Parse("127.0.0.1:2000");
            var grpcPeer = await _peerDialer.DialPeerAsync(endpoint);
            
            grpcPeer.ShouldBeNull();
        }
    }
}