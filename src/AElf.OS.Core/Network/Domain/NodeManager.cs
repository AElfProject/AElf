using System;
using System.Collections.Concurrent;
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
            return Task.FromResult(_nodes.TryAdd(node.Pubkey.ToHex(), node));
        }
        
        public async Task<NodeList> AddNodesAsync(NodeList nodes)
        {
            NodeList addedNodes = new NodeList();
            
            foreach (var node in nodes.Nodes)
            {
                if (await AddNodeAsync(node))
                    addedNodes.Nodes.Add(node);
            }
            
            return addedNodes;
        }
        
        public Task<NodeList> GetRandomNodesAsync(int maxCount)
        {
            Random rnd = new Random();
            
            var randomPeers = _nodes.OrderBy(x => rnd.Next()).Take(maxCount).Select(n => n.Value).ToList();
            
            NodeList nodes = new NodeList();
            nodes.Nodes.AddRange(randomPeers);

            return Task.FromResult(nodes);;
        }
    }
}