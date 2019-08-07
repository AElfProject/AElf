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
            var grpcUri = "ipv4:1.2.3.4:1234";

            var firstPeerFromHostResp = await _connectionService.DialBackAsync(grpcUri, GetDefaultConnectionInfo());
            firstPeerFromHostResp.Error.ShouldBe(ConnectError.ConnectOk);
            
            var secondPeerFromHostResp = await _connectionService.DialBackAsync(grpcUri, GetDefaultConnectionInfo());
            secondPeerFromHostResp.Error.ShouldBe(ConnectError.ConnectionRefused);
        }
        
        [Fact]
        public async Task DialBackPeer_LocalhostMaxHostIgnored_Test()
        {
            var grpcUriLocal = "ipv4:localhost:1234";
            var grpcUriLocalhostIp = "ipv4:127.0.0.1:1234";

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