using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.OS.Network.Domain;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class NodeManagerTests : NetworkInfrastructureTestBase
    {
        private readonly INodeManager _nodeManager;

        public NodeManagerTests()
        {
            _nodeManager = GetRequiredService<INodeManager>();
        }

        [Fact]
        public async Task AddNode_Test()
        {
            var node = GenerateTestNodes(1).First();
            var result = await _nodeManager.AddNodeAsync(node);
            result.ShouldBeTrue();

            var data = node.ToDiagnosticString();
            data.ShouldContain("endpoint");
            data.ShouldContain(node.Pubkey.ToHex().Substring(0, 45));

            //add duplicate one
            var result1 = await _nodeManager.AddNodeAsync(node);
            result1.ShouldBeFalse();
        }

        [Fact]
        public async Task AddNodes_Test()
        {
            var nodes = new NodeList
            {
                Nodes = {GenerateTestNodes(3).ToArray()}
            };
            var result = await _nodeManager.AddNodesAsync(nodes);
            result.Nodes.Count.ShouldBe(3);

            var data = nodes.ToDiagnosticString();
            foreach (var node in nodes.Nodes)
            {
                data.ShouldContain(node.Endpoint);
            }
        }

        [Fact]
        public async Task GetRandomNodes_Test()
        {
            await AddNodes_Test();

            var randomNode = await _nodeManager.GetRandomNodesAsync(1);
            randomNode.Nodes.Count.ShouldBe(1);
            
            randomNode = await _nodeManager.GetRandomNodesAsync(3);
            randomNode.Nodes.Count.ShouldBe(3);
            
            randomNode = await _nodeManager.GetRandomNodesAsync(5);
            randomNode.Nodes.Count.ShouldBe(3);
        }

        private List<NodeInfo> GenerateTestNodes(int count)
        {
            var list = new List<NodeInfo>();
            for (var i = 0; i < count; i++)
            {
                var node = new NodeInfo
                {
                    Endpoint = $"http://192.168.197.{i + 1}:8000",
                    Pubkey = ByteString.CopyFrom(CryptoHelper.GenerateKeyPair().PublicKey)
                };
                list.Add(node);
            }

            return list;
        }
    }
}