using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;

namespace AElf.OS.Network.Application
{
    public class PeerDiscoveryService : IPeerDiscoveryService
    {
        private readonly IPeerPool _peerPool;
        private readonly INodeManager _nodeManager;
        private readonly IAccountService _accountService;

        public ILogger<PeerDiscoveryService> Logger { get; set; }
        
        public PeerDiscoveryService(IPeerPool peerPool, INodeManager nodeManager, IAccountService accountService)
        {
            _peerPool = peerPool;
            _nodeManager = nodeManager;
            _accountService = accountService;

            Logger = NullLogger<PeerDiscoveryService>.Instance;
        }
        
        public async Task<NodeList> DiscoverNodesAsync()
        {
            var peers = _peerPool.GetPeers()
                .OrderBy(x => RandomHelper.GetRandom())
                .Take(NetworkConstants.DefaultDiscoveryPeersToRequestCount)
                .ToList();

            var result = new NodeList();
            var discoveredNodes = new Dictionary<string, NodeInfo>();
            
            foreach (var peer in peers)
            {
                try
                {
                    var nodes = await peer.GetNodesAsync();
                    
                    if (nodes != null && nodes.Nodes.Count > 0)
                    {
                        Logger.LogDebug($"Discovery: {peer} responded with the following nodes: {nodes}.");
                        
                        await _nodeManager.AddOrUpdateNodesAsync(nodes);

                        foreach (var node in nodes.Nodes)
                        {
                            var nodePubkey = node.Pubkey.ToHex();
                            if (_peerPool.FindPeerByPublicKey(nodePubkey) != null)
                                continue;

                            if (!discoveredNodes.ContainsKey(nodePubkey))
                            {
                                discoveredNodes.Add(nodePubkey, node);
                            }
                        }
                    }
                    else
                    {
                        Logger.LogDebug($"Discovery: {peer} responded with no nodes.");
                    }
                }
                catch (NetworkException ex)
                {
                    Logger.LogError(ex, $"Error during discover - {peer}.");
                }
            }

            if (discoveredNodes.Count <= 0)
                return result;
            
            // Check that a peer did not send us this node
            var localPubKey = await _accountService.GetPublicKeyAsync();
            var hexPubkey = localPubKey.ToHex();
            discoveredNodes.Remove(hexPubkey);
            result.Nodes.AddRange(discoveredNodes.Values);
            return result;
        }

        public async Task AddNodeAsync(NodeInfo nodeInfo)
        {
            await _nodeManager.AddOrUpdateNodeAsync(nodeInfo);
        }

        public Task<NodeList> GetNodesAsync(int maxCount)
        {
            return _nodeManager.GetRandomNodesAsync(maxCount);
        }
    }
}