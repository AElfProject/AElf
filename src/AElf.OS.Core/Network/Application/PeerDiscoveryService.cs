using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
            Random rnd = new Random();
            
            var peers = _peerPool.GetPeers()
                .OrderBy(x => rnd.Next())
                .Take(NetworkConstants.DefaultDiscoveryPeersToRequestCount)
                .ToList();

            var discoveredNodes = new NodeList();
            
            foreach (var peer in peers)
            {
                try
                {
                    var nodes = await peer.GetNodesAsync();
                    
                    if (nodes != null && nodes.Nodes.Count > 0)
                    {
                        Logger.LogDebug($"Discovery: {peer} responded with the following nodes: {nodes}.");
                        
                        var added = await _nodeManager.AddNodesAsync(nodes);
                    
                        if (added != null)
                            discoveredNodes.Nodes.AddRange(added.Nodes);
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

            if (discoveredNodes.Nodes.Count <= 0)
                return discoveredNodes;
            
            // Check that a peer did not send us this node
            var localPubKey = await _accountService.GetPublicKeyAsync();
            string hexPubkey = localPubKey.ToHex();
            discoveredNodes.Nodes.RemoveAll(n => n.Pubkey.ToHex().Equals(hexPubkey));
            
            return discoveredNodes;
        }

        public async Task AddNodeAsync(NodeInfo nodeInfo)
        {
            await _nodeManager.AddNodeAsync(nodeInfo);
        }

        public Task<NodeList> GetNodesAsync(int maxCount)
        {
            return _nodeManager.GetRandomNodesAsync(maxCount);
        }
    }
}