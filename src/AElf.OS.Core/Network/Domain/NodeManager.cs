using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Domain
{
    public interface INodeManager
    {
        Task<bool> AddNodeAsync(Node node);
        Task<NodeList> AddNodesAsync(NodeList node);
        Task<NodeList> GetRandomNodesAsync(int maxCount);
    }
    
    public class NodeManager : INodeManager, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Node> _nodes;
        public ILogger<NodeManager> Logger { get; set;}

        public NodeManager()
        {
            _nodes = new ConcurrentDictionary<string, Node>();
            
            Logger = NullLogger<NodeManager>.Instance;
        }

        public Task<bool> AddNodeAsync(Node node)
        {
            string pubKey = node.Pubkey.ToHex();
            return Task.FromResult(_nodes.TryAdd(pubKey, node));
        }
        
        public Task<NodeList> AddNodesAsync(NodeList nodes)
        {
            NodeList addedNodes = new NodeList();
            foreach (var node in nodes.Nodes)
            {
                string pubKey = node.Pubkey.ToHex();

                if (_nodes.TryAdd(pubKey, node))
                {
                    addedNodes.Nodes.Add(node);
                }
            }
            
            return Task.FromResult(addedNodes);
        }
        
        public Task<NodeList> GetRandomNodesAsync(int maxCount)
        {
            Random rnd = new Random();
            
            List<Node> randomPeers = _nodes.Select(n => new { n.Key, n.Value})
                .OrderBy(x => rnd.Next())
                .Take(maxCount)
                .Select(n => new Node
                {
                    Pubkey = n.Key.ToByteString(),
                    Endpoint = n.Value.Endpoint
                })
                .ToList();
            
            NodeList nodes = new NodeList();
            nodes.Nodes.AddRange(randomPeers);

            return Task.FromResult(nodes);;
        }
    }
}