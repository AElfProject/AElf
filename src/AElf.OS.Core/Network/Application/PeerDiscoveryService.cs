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
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<PeerDiscoveryService> Logger { get; set; }
        
        public PeerDiscoveryService(IPeerPool peerPool, INodeManager nodeManager, IAccountService accountService,
            ITaskQueueManager taskQueueManager)
        {
            _peerPool = peerPool;
            _nodeManager = nodeManager;
            _accountService = accountService;
            _taskQueueManager = taskQueueManager;

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
                    await HandleNetworkException(peer, ex);
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
        
        private async Task HandleNetworkException(IPeer peer, NetworkException exception)
        {
            if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
            {
                Logger.LogError($"Removing unrecoverable {peer.IpAddress}.");
                await _peerPool.RemovePeerAsync(peer.Info.Pubkey, false);
            }
            else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
            {
                Logger.LogError($"Queuing peer for reconnection {peer.IpAddress}.");
                QueueNetworkTask(async () => await RecoverPeerAsync(peer));
            }
        }
        
        private void QueueNetworkTask(Func<Task> task)
        {
            _taskQueueManager.Enqueue(task, NetworkConstants.PeerReconnectionQueueName);
        }
        
        private async Task RecoverPeerAsync(IPeer peer)
        {
            if (peer.IsReady) // peer recovered already
                return;
                
            var success = await peer.TryRecoverAsync();

            if (!success)
                await _peerPool.RemovePeerAsync(peer.Info.Pubkey, false);
        }

        public Task<NodeList> GetNodesAsync(int maxCount)
        {
            return _nodeManager.GetRandomNodesAsync(maxCount);
        }
    }
}