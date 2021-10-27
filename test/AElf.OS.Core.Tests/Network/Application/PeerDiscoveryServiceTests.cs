using System.Threading.Tasks;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Application
{
    public class PeerDiscoveryServiceTests : PeerDiscoveryTestBase
    {
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly INodeManager _nodeManager;
        private readonly IDiscoveredNodeCacheProvider _discoveredNodeCacheProvider;
        private readonly IPeerDiscoveryJobProcessor _peerDiscoveryJobProcessor;

        public PeerDiscoveryServiceTests()
        {
            _peerDiscoveryService = GetRequiredService<IPeerDiscoveryService>();
            _nodeManager = GetRequiredService<INodeManager>();
            _discoveredNodeCacheProvider = GetRequiredService<IDiscoveredNodeCacheProvider>();
            _peerDiscoveryJobProcessor = GetRequiredService<IPeerDiscoveryJobProcessor>();
        }

        [Fact]
        public async Task DiscoverNodes_Test()
        {
            await _peerDiscoveryService.DiscoverNodesAsync();
            await _peerDiscoveryJobProcessor.CompleteAsync();

            var nodeList = await _nodeManager.GetRandomNodesAsync(10);
            nodeList.Nodes.Count.ShouldBe(1);
            nodeList.Nodes[0].Endpoint.ShouldBe("192.168.100.100:8003");
            nodeList.Nodes[0].Pubkey.ShouldBe(ByteString.CopyFromUtf8("192.168.100.100:8003"));
        }

        [Fact]
        public async Task RefreshNode_Test()
        {
            var node1 = new NodeInfo
            {
                Endpoint = "192.168.100.1:8000",
                Pubkey = ByteString.CopyFromUtf8("node1")
            };
            await _peerDiscoveryService.AddNodeAsync(node1);
            
            var node2 = new NodeInfo
            {
                Endpoint = "192.168.100.1:8001",
                Pubkey = ByteString.CopyFromUtf8("node2")
            };
            await _peerDiscoveryService.AddNodeAsync(node2);

            await _peerDiscoveryService.RefreshNodeAsync();
            
            var nodeList = await _nodeManager.GetRandomNodesAsync(10);
            nodeList.Nodes.Count.ShouldBe(2);
            nodeList.Nodes.ShouldContain(node1);
            nodeList.Nodes.ShouldContain(node2);
            
            await _peerDiscoveryService.RefreshNodeAsync();
            
            nodeList = await _nodeManager.GetRandomNodesAsync(10);
            nodeList.Nodes.Count.ShouldBe(1);
            nodeList.Nodes[0].ShouldBe(node1);

            _discoveredNodeCacheProvider.TryTake(out var endpoint);
            endpoint.ShouldBe(node1.Endpoint);
            
            _discoveredNodeCacheProvider.TryTake(out endpoint);
            endpoint.ShouldBeNull();
        }

        [Fact]
        public async Task AddNode_And_GetNodes_Test()
        {
            var node = new NodeInfo
            {
                Endpoint = "192.168.197.1:8000",
                Pubkey = ByteString.CopyFromUtf8("test")
            };

            await _peerDiscoveryService.AddNodeAsync(node);
            var result = await _nodeManager.GetRandomNodesAsync(10);
            result.Nodes.Count.ShouldBe(1);
            result.Nodes[0].ShouldBe(node);

            _discoveredNodeCacheProvider.TryTake(out var endpoint);
            endpoint.ShouldBe(node.Endpoint);
            
            _discoveredNodeCacheProvider.TryTake(out endpoint);
            endpoint.ShouldBeNull();
        }
    }
}