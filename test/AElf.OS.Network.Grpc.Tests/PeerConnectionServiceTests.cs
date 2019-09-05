using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Grpc;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerConnectionServiceTests : ServerServiceTestBase
    {
        private readonly IConnectionService _connectionService;

        public PeerConnectionServiceTests()
        {
            _connectionService = GetRequiredService<IConnectionService>();
        }

        [Fact]
        public async Task DialBackPeer_MaxPeersPerHostReached_Test()
        {
            var peerEndpoint = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234);

            var firstPeerFromHostResp = await _connectionService.DialBackAsync(peerEndpoint, GetDefaultConnectionInfo());
            firstPeerFromHostResp.Error.ShouldBe(ConnectError.ConnectOk);
            
            var secondPeerFromHostResp = await _connectionService.DialBackAsync(peerEndpoint, GetDefaultConnectionInfo());
            secondPeerFromHostResp.Error.ShouldBe(ConnectError.ConnectionRefused);
        }
        
        [Fact]
        public async Task DialBackPeer_LocalhostMaxHostIgnored_Test()
        {
            var grpcUriLocal = new IPEndPoint(IPAddress.Loopback, 1234);
            var grpcUriLocalhostIp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);

            var firstLocalPeerResp = await _connectionService.DialBackAsync(grpcUriLocal, GetDefaultConnectionInfo());
            firstLocalPeerResp.Error.ShouldBe(ConnectError.ConnectOk);
            
            var secondLocalPeerResp = await _connectionService.DialBackAsync(grpcUriLocal, GetDefaultConnectionInfo());
            secondLocalPeerResp.Error.ShouldBe(ConnectError.ConnectOk);
            
            var thirdPeerFromHostResp = await _connectionService.DialBackAsync(grpcUriLocalhostIp, GetDefaultConnectionInfo());
            thirdPeerFromHostResp.Error.ShouldBe(ConnectError.ConnectOk);
        }

        private ConnectionInfo GetDefaultConnectionInfo()
        {
            return new ConnectionInfo { ChainId = 9992731, Version = KernelConstants.ProtocolVersion };
        }
    }
}