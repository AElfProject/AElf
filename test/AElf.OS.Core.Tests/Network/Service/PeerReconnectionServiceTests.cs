using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Service
{
    public class PeerReconnectionServiceTests : OSCoreNetworkServiceTestBase
    {
        private IReconnectionService _reconnectionService;
        private IPeerReconnectionStateProvider _connectionStateProvider;

        public PeerReconnectionServiceTests()
        {
            _reconnectionService = GetRequiredService<IReconnectionService>();
            _connectionStateProvider = GetRequiredService<IPeerReconnectionStateProvider>();
        }

        [Fact]
        public async Task ScheduledPeer_ShouldBeRetrievableWithTheProvider()
        {
            var endpoint = "127.0.0.1:5677";
            
            _reconnectionService.SchedulePeerForReconnection(endpoint, TimestampHelper.GetUtcNow());
            var peers = _connectionStateProvider.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow().AddSeconds(1));
            
            peers.Count.ShouldBe(1);
            peers.First().Endpoint.ShouldBe(endpoint);
        }
        
        [Fact]
        public async Task RemovePeer_ShouldNotRetrievableWithTheProvider()
        {
            var endpoint = "127.0.0.1:5677";
            
            _reconnectionService.SchedulePeerForReconnection(endpoint, TimestampHelper.GetUtcNow());
            _reconnectionService.RemovePeer(endpoint);
            
            var peers = _connectionStateProvider.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow().AddSeconds(1));
            peers.Count.ShouldBe(0);
        }
    }
}