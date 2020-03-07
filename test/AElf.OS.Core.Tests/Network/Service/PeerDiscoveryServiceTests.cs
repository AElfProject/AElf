using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Domain;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Service
{
    public class PeerDiscoveryServiceTests : OSCoreNetworkServiceTestBase
    {
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly INodeManager _nodeManager;

        public PeerDiscoveryServiceTests()
        {
            _peerDiscoveryService = GetRequiredService<IPeerDiscoveryService>();
            _nodeManager = GetRequiredService<INodeManager>();
        }

        [Fact]
        public async Task DiscoverNodes_Test()
        {
            var nodes = await _peerDiscoveryService.GetNodesAsync(1);
            nodes.Nodes.Count().ShouldBe(0);
            
            var discoverResult = await _peerDiscoveryService.DiscoverNodesAsync();
            discoverResult.Nodes.Count.ShouldBe(1);
            
            nodes = await _peerDiscoveryService.GetNodesAsync(1);
            nodes.Nodes.Count().ShouldBe(1);
            
            //rediscovery node again
            discoverResult = await _peerDiscoveryService.DiscoverNodesAsync();
            discoverResult.Nodes.Count.ShouldBe(1);
        }

        [Fact]
        public async Task AddNode_And_GetNodes_Test()
        {
            var node = new NodeInfo
            {
                Endpoint = "http://192.168.197.1:8000",
                Pubkey = ByteString.CopyFromUtf8("test")
            };

            await _peerDiscoveryService.AddNodeAsync(node);
            var result = await _nodeManager.GetRandomNodesAsync(1);
            result.Nodes.Contains(node).ShouldBeTrue();

            var nodes = await _peerDiscoveryService.GetNodesAsync(1);
            nodes.Nodes.Contains(node).ShouldBeTrue();
        }
    }
}