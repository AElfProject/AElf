using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public class NetworkService : INetworkService, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly INetworkQueueManager _queueManager;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool, INetworkQueueManager queueManager)
        {
            _peerPool = peerPool;
            _queueManager = queueManager;

            Logger = NullLogger<NetworkService>.Instance;
        }

        public async Task<bool> AddPeerAsync(string address)
        {
            return await _peerPool.AddPeerAsync(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            return await _peerPool.RemovePeerByAddressAsync(address);
        }

        public List<string> GetPeerIpList()
        {
            return _peerPool.GetPeers(true).Select(p => p.PeerIpAddress).ToList();
        }

        public List<IPeer> GetPeers()
        {
            return _peerPool.GetPeers(true).ToList(); 
        }

        public async Task<int> BroadcastAnnounceAsync(BlockHeader blockHeader,bool hasFork)
        {
            int successfulBcasts = 0;
            
            var announce = new PeerNewBlockAnnouncement
            {
                BlockHash = blockHeader.GetHash(),
                BlockHeight = blockHeader.Height,
                HasFork = hasFork
            };
            
            _peerPool.AddRecentBlockHeightAndHash(blockHeader.Height, blockHeader.GetHash(), hasFork);

            foreach (var peer in _peerPool.GetPeers())
            {
                var transactionQueue = _queueManager.GetQueue(NetworkConstants.AnnouncementQueueName);
                await transactionQueue.EnqueueAsync(async () =>
                {
                    await peer.AnnounceAsync(announce);
                });
            }
            
            return successfulBcasts;
        }
        
        public async Task<int> BroadcastTransactionAsync(Transaction tx)
        {
            int successfulBcasts = 0;
            
            foreach (var peer in _peerPool.GetPeers())
            {
                var transactionQueue = _queueManager.GetQueue(NetworkConstants.TransactionQueueName);
                await transactionQueue.EnqueueAsync(async () =>
                {
                    await peer.SendTransactionAsync(tx);
                });
            }
            
            return successfulBcasts;
        }

        public async Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousBlock, int count, 
            string peerPubKey = null)
        {
            var peers = SelectPeers(peerPubKey);

            var blocks = await RequestAsync(peers, p => p.GetBlocksAsync(previousBlock, count), 
                blockList => blockList != null && blockList.Count > 0, 
                peerPubKey);

            if (blocks != null && (blocks.Count == 0 || blocks.Count != count))
                Logger.LogWarning($"Block count miss match, asked for {count} but got {blocks.Count}");

            return blocks;
        }
        
        private List<IPeer> SelectPeers(string peerPubKey)
        {
            List<IPeer> peers = new List<IPeer>();
            
            // Get the suggested peer 
            IPeer suggestedPeer = _peerPool.FindPeerByPublicKey(peerPubKey);

            if (suggestedPeer == null)
                Logger.LogWarning("Could not find suggested peer");
            else
                peers.Add(suggestedPeer);
            
            // Get our best peer
            IPeer bestPeer = _peerPool.GetBestPeer();
            
            if (bestPeer == null)
                Logger.LogWarning("No best peer.");
            else if (bestPeer.PubKey != peerPubKey)
                peers.Add(bestPeer);
            
            Random rnd = new Random();
            
            // Fill with random peers.
            List<IPeer> randomPeers = _peerPool.GetPeers()
                .Where(p => p.PubKey != peerPubKey && (bestPeer == null || p.PubKey != bestPeer.PubKey))
                .OrderBy(x => rnd.Next())
                .Take(NetworkConstants.DefaultMaxRandomPeersPerRequest)
                .ToList();
            
            peers.AddRange(randomPeers);
            
            Logger.LogDebug($"Selected {peers.Count} for the request.");

            return peers;
        }
        
        public async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash, string peer = null)
        {
            Logger.LogDebug($"Getting block by hash, hash: {hash} from {peer}.");
            
            var peers = SelectPeers(peer);
            return await RequestAsync(peers, p => p.RequestBlockAsync(hash), blockWithTransactions => blockWithTransactions != null, peer);
        }

        private async Task<(IPeer, T)> DoRequest<T>(IPeer peer, Func<IPeer, Task<T>> func) where T : class
        {
            try
            {
                Logger.LogDebug($"before request send to {peer.PeerIpAddress}.");
                var res = await func(peer);
                Logger.LogDebug($"request send to {peer.PeerIpAddress}.");
                
                return (peer, res);
            }
            catch (NetworkException e)
            {
                Logger.LogError(e, $"Error while requesting block from {peer.PeerIpAddress}.");
            }
            
            return (peer, null);
        }

        private async Task<T> RequestAsync<T>(List<IPeer> peers, Func<IPeer, Task<T>> func,
            Predicate<T> validationFunc, string suggested) where T : class
        {
            if (peers.Count <= 0)
            {
                Logger.LogWarning("Peer list is empty.");
                return null;
            }
            
            var taskList = peers.Select(peer => DoRequest(peer, func)).ToList();
            
            Task<(IPeer, T)> finished = null;
            
            while (taskList.Count > 0)
            {
                var next = await Task.WhenAny(taskList);

                if (validationFunc(next.Result.Item2))
                {
                    finished = next;
                    break;
                }

                taskList.Remove(next);
            }

            if (finished == null)
            {
                Logger.LogDebug($"No peer succeeded.");
                return null;
            }

            IPeer taskPeer = finished.Result.Item1;
            T taskRes = finished.Result.Item2;
            
            UpdateBestPeer(taskPeer);
            
            if (suggested != taskPeer.PubKey)
                Logger.LogWarning($"Suggested {suggested}, used {taskPeer.PubKey}");
            
            Logger.LogDebug($"First replied {taskRes} : {taskPeer}.");

            return taskRes;
        }

        private void UpdateBestPeer(IPeer taskPeer)
        {
            if (taskPeer.IsBest) 
                return;
            
            Logger.LogDebug($"New best peer found: {taskPeer}.");

            foreach (var peerToReset in _peerPool.GetPeers(true))
            {
                peerToReset.IsBest = false;
            }
                
            taskPeer.IsBest = true;
        }

        public Task<long> GetBestChainHeightAsync(string peerPubKey = null)
        {
            var peer = !peerPubKey.IsNullOrEmpty()
                ? _peerPool.FindPeerByPublicKey(peerPubKey)
                : _peerPool.GetPeers().OrderByDescending(p => p.CurrentBlockHeight).FirstOrDefault();
            return Task.FromResult(peer?.CurrentBlockHeight ?? 0);
        }
    }
}