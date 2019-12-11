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
        Task<bool> AddOrUpdateNodeAsync(NodeInfo nodeInfo);
        Task<NodeList> AddOrUpdateNodesAsync(NodeList node);
        Task<NodeList> GetRandomNodesAsync(int maxCount);
    }
    
    public class NodeManager : INodeManager, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, NodeInfo> _nodes;
        public ILogger<NodeManager> Logger { get; set;}

        public NodeManager()
        {
            _nodes = new ConcurrentDictionary<string, NodeInfo>();
            
            Logger = NullLogger<NodeManager>.Instance;
        }

        public Task<bool> AddOrUpdateNodeAsync(NodeInfo nodeInfo)
        {
            bool addedOrUpdated = true;
            _nodes.AddOrUpdate(nodeInfo.Pubkey.ToHex(), nodeInfo, (pubkey, oldInfo) =>
            {
                if (oldInfo.Endpoint != nodeInfo.Endpoint) 
                    return nodeInfo;
                
                addedOrUpdated = false;
                return oldInfo;
            });

            return Task.FromResult(addedOrUpdated);
        }
        
        public async Task<NodeList> AddOrUpdateNodesAsync(NodeList nodes)
        {
            NodeList addedNodes = new NodeList();
            
            foreach (var node in nodes.Nodes)
            {
                if (await AddOrUpdateNodeAsync(node))
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

            return Task.FromResult(nodes);
        }
    }
}