using System.Threading.Tasks;
using AElf.Cryptography;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Domain
{
    public class NodeManagerTests : NetworkInfrastructureTestBase
    {
        private readonly INodeManager _nodeManager;
        private readonly NetworkOptions _networkOptions;

        public NodeManagerTests()
        {
            _nodeManager = GetRequiredService<INodeManager>();
            _networkOptions = GetRequiredService<IOptionsSnapshot<NetworkOptions>>().Value;
        }

        [Fact]
        public async Task AddNode_Test()
        {
            var node = GenerateTestNode(100);
            var addResult = await _nodeManager.AddNodeAsync(node);
            addResult.ShouldBeTrue();
            
            addResult = await _nodeManager.AddNodeAsync(node);
            addResult.ShouldBeFalse();
            
            for (int i = 0; i < _networkOptions.PeerDiscoveryMaxNodesToKeep - 1; i++)
            {
                node = GenerateTestNode(i+1);
                addResult = await _nodeManager.AddNodeAsync(node);
                addResult.ShouldBeTrue();
            }
            
            node = GenerateTestNode(101);
            addResult = await _nodeManager.AddNodeAsync(node);
            addResult.ShouldBeFalse();
        }

        [Fact]
        public async Task GetRandomNodes_Test()
        {
            var randomNode = await _nodeManager.GetRandomNodesAsync(1);
            randomNode.Nodes.Count.ShouldBe(0);
            
            for (int i = 0; i < 3; i++)
            {
                var node = GenerateTestNode(i+1);
                await _nodeManager.AddNodeAsync(node);
            }

            randomNode = await _nodeManager.GetRandomNodesAsync(1);
            randomNode.Nodes.Count.ShouldBe(1);

            randomNode = await _nodeManager.GetRandomNodesAsync(3);
            randomNode.Nodes.Count.ShouldBe(3);
            
            randomNode = await _nodeManager.GetRandomNodesAsync(5);
            randomNode.Nodes.Count.ShouldBe(3);
        }

        [Fact]
        public async Task GetNode_Test()
        {
            var node = GenerateTestNode(100);
            await _nodeManager.AddNodeAsync(node);
            
            var result = await _nodeManager.GetNodeAsync("192.168.100.1:80");
            result.ShouldBeNull();

            result = await _nodeManager.GetNodeAsync(node.Endpoint);
            result.ShouldBe(node);
        }

        [Fact]
        public async Task UpdateNode_Test()
        {
            var node = GenerateTestNode(100);
            await _nodeManager.AddNodeAsync(node);
            
            var newNode = GenerateTestNode(100);
            await _nodeManager.UpdateNodeAsync(newNode);

            var result = await _nodeManager.GetNodeAsync(node.Endpoint);
            result.ShouldBe(newNode);
            
            var newNode2 = GenerateTestNode(200);
            await _nodeManager.UpdateNodeAsync(newNode2);

            result = await _nodeManager.GetNodeAsync(node.Endpoint);
            result.ShouldBe(newNode);
        }

        [Fact]
        public async Task RemoveNode_Test()
        {
            var node = GenerateTestNode(100);
            await _nodeManager.AddNodeAsync(node);

            await _nodeManager.RemoveNodeAsync(node.Endpoint);
            
            var result = await _nodeManager.GetNodeAsync(node.Endpoint);
            result.ShouldBeNull();
        }

        private NodeInfo GenerateTestNode(int port)
        {
            var node = new NodeInfo
            {
                Endpoint = $"192.168.197.100:{port}",
                Pubkey = ByteString.CopyFrom(CryptoHelper.GenerateKeyPair().PublicKey)
            };

            return node;
        }
    }
}