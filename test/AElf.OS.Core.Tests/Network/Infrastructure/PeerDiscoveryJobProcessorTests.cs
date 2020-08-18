using System.Threading.Tasks;
using AElf.OS.Network.Domain;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class PeerDiscoveryJobProcessorTests : PeerDiscoveryTestBase
    {
        private readonly IPeerDiscoveryJobProcessor _peerDiscoveryJobProcessor;
        private readonly IPeerPool _peerPool;
        private readonly INodeManager _nodeManager;
        private readonly IDiscoveredNodeCacheProvider _discoveredNodeCacheProvider;
        private readonly NetworkOptions _networkOptions;

        public PeerDiscoveryJobProcessorTests()
        {
            _peerDiscoveryJobProcessor = GetRequiredService<IPeerDiscoveryJobProcessor>();
            _peerPool = GetRequiredService<IPeerPool>();
            _nodeManager = GetRequiredService<INodeManager>();
            _discoveredNodeCacheProvider = GetRequiredService<IDiscoveredNodeCacheProvider>();
            _networkOptions = GetRequiredService<IOptionsSnapshot<NetworkOptions>>().Value;
        }

        [Fact]
        public async Task SendDiscoveryJob_NoNode_Test()
        {
            var peer = _peerPool.FindPeerByPublicKey("PeerWithNoNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task SendDiscoveryJob_SamePubkeyNode_Test()
        {
            var peer = _peerPool.FindPeerByPublicKey("PeerWithSamePubkeyNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task SendDiscoveryJob_UnavailableNode_Test()
        {
            var peer = _peerPool.FindPeerByPublicKey("PeerWithUnavailableNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task SendDiscoveryJob_Test()
        {
            var peer = _peerPool.FindPeerByPublicKey("PeerWithNormalNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(1);
            nodes.Nodes[0].Endpoint.ShouldBe("192.168.100.100:8003");
        }
        
        [Fact]
        public async Task SendDiscoveryJob_NodeExist_Test()
        {
            var oldNode = new NodeInfo
            {
                Endpoint = "192.168.100.100:8003", 
                Pubkey = ByteString.CopyFromUtf8("OldPubkey")
            };
            await _nodeManager.AddNodeAsync(oldNode);
            _discoveredNodeCacheProvider.Add(oldNode.Endpoint);
            
            var peer = _peerPool.FindPeerByPublicKey("PeerWithNormalNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(1);
            nodes.Nodes[0].Endpoint.ShouldBe("192.168.100.100:8003");
            nodes.Nodes[0].Pubkey.ShouldBe(ByteString.CopyFromUtf8("192.168.100.100:8003"));
        }
        
        [Fact]
        public async Task SendDiscoveryJob_NodeManagerIsFull_LocalAvailable_Test()
        {
            for (int i = 0; i < _networkOptions.PeerDiscoveryMaxNodesToKeep; i++)
            {
                var nodeInfo = new NodeInfo
                {
                    Endpoint = "192.168.100.1:800"+i, 
                    Pubkey = ByteString.CopyFromUtf8("pubkey")
                };
                await _nodeManager.AddNodeAsync(nodeInfo);
                _discoveredNodeCacheProvider.Add(nodeInfo.Endpoint);
            }

            var peer = _peerPool.FindPeerByPublicKey("PeerWithNormalNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(5);

            var node = await _nodeManager.GetNodeAsync("192.168.100.100:8003");
            node.ShouldBeNull();
        }
        
        [Fact]
        public async Task SendDiscoveryJob_NodeManagerIsFull_LocalUnavailable_Test()
        {
            for (int i = 0; i < _networkOptions.PeerDiscoveryMaxNodesToKeep; i++)
            {
                var nodeInfo = new NodeInfo
                {
                    Endpoint = "192.168.100.1:800"+(i+1), 
                    Pubkey = ByteString.CopyFromUtf8("pubkey")
                };
                await _nodeManager.AddNodeAsync(nodeInfo);
                _discoveredNodeCacheProvider.Add(nodeInfo.Endpoint);
            }

            var peer = _peerPool.FindPeerByPublicKey("PeerWithNormalNode");
            await SendDiscoveryJobAsync(peer);

            var nodes = await _nodeManager.GetRandomNodesAsync(10);
            nodes.Nodes.Count.ShouldBe(5);

            var node = await _nodeManager.GetNodeAsync("192.168.100.100:8003");
            node.ShouldNotBeNull();

            for (int i = 0; i < _networkOptions.PeerDiscoveryMaxNodesToKeep -1; i++)
            {
                _discoveredNodeCacheProvider.TryTake(out var endpoint);
                endpoint.ShouldBe("192.168.100.1:800" + (i + 2));
            }
           
            _discoveredNodeCacheProvider.TryTake(out var newEndpoint);
            newEndpoint.ShouldBe("192.168.100.100:8003");
        }

        private async Task SendDiscoveryJobAsync(IPeer peer)
        {
            await _peerDiscoveryJobProcessor.SendDiscoveryJobAsync(peer);
            await _peerDiscoveryJobProcessor.CompleteAsync();
        }
    }
}