using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Domain
{
    public interface INodeManager
    {
        Task<bool> AddNodeAsync(NodeInfo nodeInfo);
        Task<NodeList> GetRandomNodesAsync(int maxCount);
        Task<NodeInfo> GetNodeAsync(string endpoint);
        Task UpdateNodeAsync(NodeInfo nodeInfo);
        Task RemoveNodeAsync(string endpoint);
    }
    
    public class NodeManager : INodeManager, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, NodeInfo> _nodes;
        public ILogger<NodeManager> Logger { get; set;}
        private readonly NetworkOptions _networkOptions;

        public NodeManager(IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _nodes = new ConcurrentDictionary<string, NodeInfo>();
            
            _networkOptions = networkOptions.Value;
            Logger = NullLogger<NodeManager>.Instance;
        }

        public Task<bool> AddNodeAsync(NodeInfo nodeInfo)
        {
            if (_nodes.Count >= _networkOptions.PeerDiscoveryMaxNodesToKeep)
                return Task.FromResult(false);

            var addResult = _nodes.TryAdd(nodeInfo.Endpoint, nodeInfo);
            return Task.FromResult(addResult);
        }

        public Task<NodeList> GetRandomNodesAsync(int maxCount)
        {
            var randomPeers = _nodes.OrderBy(x => RandomHelper.GetRandom()).Take(maxCount).Select(n => n.Value)
                .ToList();

            NodeList nodes = new NodeList();
            nodes.Nodes.AddRange(randomPeers);

            return Task.FromResult(nodes);
        }

        public Task<NodeInfo> GetNodeAsync(string endpoint)
        {
            _nodes.TryGetValue(endpoint, out var nodeInfo);
            return Task.FromResult(nodeInfo);
        }

        public Task UpdateNodeAsync(NodeInfo nodeInfo)
        {
            if (_nodes.TryGetValue(nodeInfo.Endpoint, out var oldNodeInfo) && oldNodeInfo.Pubkey!=nodeInfo.Pubkey)
            {
                _nodes.TryUpdate(nodeInfo.Endpoint, nodeInfo, oldNodeInfo);
            }

            return Task.CompletedTask;
        }

        public Task RemoveNodeAsync(string endpoint)
        {
            _nodes.TryRemove(endpoint, out _);
            return Task.CompletedTask;
        }
    }
}