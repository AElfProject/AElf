using System.Threading.Tasks;
using AElf.OS.Network.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class ConnectionServiceBootNodeTests : GrpcNetworkConnectionWithBootNodesTestBase
    {
        private readonly IConnectionService _connectionService;
        private readonly IReconnectionService _reconnectionService;

        public ConnectionServiceBootNodeTests()
        {
            _connectionService = GetRequiredService<IConnectionService>();
            _reconnectionService = GetRequiredService<IReconnectionService>();
        }

        [Fact]
        public async Task TrySchedulePeerReconnection_IsInboundAndInBootNode_Test()
        {
            var peer = GrpcTestPeerHelper.CreateBasicPeer("127.0.0.1:2020", NetworkTestConstants.FakePubkey);
            peer.Info.IsInbound = true;

            var result = await _connectionService.TrySchedulePeerReconnectionAsync(peer);
            result.ShouldBeTrue();

            peer.IsConnected.ShouldBeFalse();
            peer.IsShutdown.ShouldBeTrue();

            _reconnectionService.GetReconnectingPeer("127.0.0.1:2020").ShouldNotBeNull();
        }
    }
}