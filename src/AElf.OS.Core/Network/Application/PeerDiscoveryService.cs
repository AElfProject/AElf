using System.Threading.Tasks;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.Network.Application
{
    public class PeerDiscoveryService : IPeerDiscoveryService
    {
        private readonly IPeerPool _peerPool;
        private INodeManager _nodeManager;

        public ILogger<PeerDiscoveryService> Logger { get; set; }
        
        public PeerDiscoveryService(IPeerPool peerPool, INodeManager nodeManager)
        {
            _peerPool = peerPool;
            _nodeManager = nodeManager;

            Logger = NullLogger<PeerDiscoveryService>.Instance;
        }
        
        public async Task<NodeList> UpdatePeersAsync()
        {
            var peers = _peerPool.GetPeers();

            var discoveredNodes = new NodeList();
            foreach (var peer in peers)
            {
                try
                {
                    var nodes = await peer.GetNodesAsync();
                    var added = await _nodeManager.AddNodesAsync(nodes);
                    
                    if (added != null)
                        discoveredNodes.Nodes.AddRange(added.Nodes);
                }
                catch (NetworkException ex)
                {
                    Logger.LogError(ex, $"Error during discover - {peer}.");
                }
            }
            
            return discoveredNodes;
        }

        public Task<NodeList> GetNodesAsync(int maxCount)
        {
            return _nodeManager.GetRandomNodes(maxCount);
        }
    }
}