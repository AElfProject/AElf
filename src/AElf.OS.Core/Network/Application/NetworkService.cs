using System;
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
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool, ITaskQueueManager taskQueueManager)
        {
            _peerPool = peerPool;
            _taskQueueManager = taskQueueManager;

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
            
            var peers = _peerPool.GetPeers().ToList();

            _peerPool.AddRecentBlockHeightAndHash(blockHeader.Height, blockHeader.GetHash(), hasFork);
            
            Logger.LogDebug("About to broadcast to peers.");
            
            var tasks = peers.Select(peer => DoAnnounce(peer, announce)).ToList();
            await Task.WhenAll(tasks);

            foreach (var finishedTask in tasks.Where(t => t.IsCompleted))
            {
                if (finishedTask.Result)
                    successfulBcasts++;
            }
            
            Logger.LogDebug("Broadcast successful !");
            
            return successfulBcasts;
        }

        public async Task<int> BroadcastPreLibAnnounceAsync(long blockHeight, Hash blockHash,int preLibCount)
        {
            var successfulBcasts = 0;
            
            var hasBlock = _peerPool.RecentBlockHeightAndHashMappings.TryGetValue(blockHeight, out var hash) &&
                           hash == blockHash;
            if (!hasBlock) return successfulBcasts;

            var announce = new PeerPreLibAnnouncement
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight,
                PreLibCount = preLibCount
            };

            var peers = _peerPool.GetPeers().ToList();

            _peerPool.AddPreLibBlockHeightAndHash(announce.BlockHeight, announce.BlockHash, preLibCount);

            Logger.LogDebug("About to broadcast pre lib to peers.");

            var tasks = peers.Select(peer => DoPreLibAnnounce(peer, announce)).ToList();
            await Task.WhenAll(tasks);

            foreach (var finishedTask in tasks.Where(t => t.IsCompleted))
            {
                if (finishedTask.Result)
                    successfulBcasts++;
            }

            Logger.LogDebug("Broadcast pre lib successful !");

            return successfulBcasts;
        }

        private async Task<bool> DoAnnounce(IPeer peer, PeerNewBlockAnnouncement announce)
        {
            try
            {
                Logger.LogDebug($"Before broadcast {announce.BlockHash} to {peer}.");
                await peer.AnnounceAsync(announce);
                Logger.LogDebug($"After broadcast {announce.BlockHash} to {peer}.");

                return true;
            }
            catch (NetworkException ex)
            {
                Logger.LogError(ex, "Error while announcing.");
                await HandleNetworkException(peer, ex);
            }

            return false;
        }
        
        private async Task<bool> DoPreLibAnnounce(IPeer peer, PeerPreLibAnnouncement announce)
        {
            try
            {
                Logger.LogDebug($"Before broadcast pre lib {announce.BlockHash} to {peer}.");
                await peer.PreLibAnnounceAsync(announce);
                Logger.LogDebug($"After broadcast pre lib {announce.BlockHash} to {peer}.");

                return true;
            }
            catch (NetworkException e)
            {
                Logger.LogError(e, "Error while sending block.");
            }

            return false;
        }
        
        public async Task<int> BroadcastTransactionAsync(Transaction tx)
        {
            int successfulBcasts = 0;
            
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    await peer.SendTransactionAsync(tx);
                    
                    successfulBcasts++;
                }
                catch (NetworkException ex)
                {
                    Logger.LogError(ex, "Error while sending transaction.");
                    await HandleNetworkException(peer, ex);
                }
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
            catch (NetworkException ex)
            {
                Logger.LogError(ex, $"Error while requesting block from {peer.PeerIpAddress}.");
                await HandleNetworkException(peer, ex);
            }
            
            return (peer, null);
        }

        private async Task HandleNetworkException(IPeer peer, NetworkException exception)
        {
            if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
            {
                await _peerPool.RemovePeerAsync(peer.PubKey, false);
            }
            else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
            {
                Logger.LogError(exception, $"Queuing for reconnection {peer.PeerIpAddress}.");
                QueueConnectionWait(peer);
            }
        }

        private void QueueConnectionWait(IPeer peer)
        {
            _taskQueueManager.Enqueue(async () =>
            {
                if (peer.IsReady) // peer recovered already
                    return;
                
                var success = await peer.TryWaitForStateChangedAsync();

                if (!success)
                    await _peerPool.RemovePeerAsync(peer.PubKey, false);
                
            }, NetworkConstants.PeerReconnectionQueueName);
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