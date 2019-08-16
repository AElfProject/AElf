using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    /// <summary>
    /// Exposes networking functionality to the application handlers.
    /// </summary>
    public class NetworkService : INetworkService, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IAElfNetworkServer _networkServer;
        private readonly IKnownBlockCacheProvider _knownBlockCacheProvider;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool, ITaskQueueManager taskQueueManager, IAElfNetworkServer networkServer, 
            IKnownBlockCacheProvider knownBlockCacheProvider)
        {
            _peerPool = peerPool;
            _taskQueueManager = taskQueueManager;
            _networkServer = networkServer;
            _knownBlockCacheProvider = knownBlockCacheProvider;

            Logger = NullLogger<NetworkService>.Instance;
        }

        public async Task<bool> AddPeerAsync(string address)
        {
            return await _networkServer.ConnectAsync(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            var peer = _peerPool.FindPeerByAddress(address);
            if (peer == null)
            {
                Logger.LogWarning($"Could not find peer at address {address}");
                return false;
            }

            await _networkServer.DisconnectAsync(peer);
            return true;
        }

        public List<IPeer> GetPeers()
        {
            return _peerPool.GetPeers(true).ToList();
        }

        private bool IsOldBlock(BlockHeader header)
        {
            var limit = TimestampHelper.GetUtcNow() 
                        - TimestampHelper.DurationFromMinutes(NetworkConstants.DefaultMaxBlockAgeToBroadcastInMinutes);
            
            if (header.Time < limit)
                return true;

            return false;
        }
        
        /// <summary>
        /// returns false if the block was unknown, false if already known.
        /// </summary>
        private bool TryAddKnownBlock(BlockHeader blockHeader)
        {
            var blockHash = blockHeader.GetHash();
            if (_knownBlockCacheProvider.TryGetBlockByHeight(blockHeader.Height, out var recentBlockHash) &&
                recentBlockHash == blockHash)
            {
                Logger.LogDebug($"BlockHeight: {blockHeader.Height}, BlockHash: {blockHash} has been broadcast.");
                return false;
            }
            
            _knownBlockCacheProvider.AddKnownBlock(blockHeader.Height, blockHash, false);

            return true;
        }
        
        public Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
        {
            if (!TryAddKnownBlock(blockWithTransactions.Header))
                return Task.CompletedTask;
            
            if (IsOldBlock(blockWithTransactions.Header))
                return Task.CompletedTask;
            
            _taskQueueManager.Enqueue(async () =>
            {
                foreach (var peer in _peerPool.GetPeers())
                {
                    try
                    {
                        await peer.SendBlockAsync(blockWithTransactions);
                    }
                    catch (NetworkException ex)
                    {
                        Logger.LogError(ex, $"Error while broadcasting block to {peer}.");
                        await HandleNetworkException(peer, ex);
                    }
                }
                
            }, NetworkConstants.BlockBroadcastQueueName);
            
            return Task.CompletedTask;
        }

        public Task BroadcastAnnounceAsync(BlockHeader blockHeader, bool hasFork)
        {
            var blockHash = blockHeader.GetHash();
            
            if (!TryAddKnownBlock(blockHeader))
                return Task.CompletedTask;
            
            if (IsOldBlock(blockHeader))
                return Task.CompletedTask;
            
            var announce = new BlockAnnouncement
            {
                BlockHash = blockHash,
                BlockHeight = blockHeader.Height,
                HasFork = hasFork
            };
            
            var beforeEnqueue = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                var execTime = TimestampHelper.GetUtcNow();
                if (execTime > beforeEnqueue +
                    TimestampHelper.DurationFromMilliseconds(NetworkConstants.AnnouncementQueueJobTimeout))
                {
                    Logger.LogWarning($"Announcement too old: {execTime - beforeEnqueue}");
                    return;
                }
                
                foreach (var peer in _peerPool.GetPeers())
                {
                    try
                    {
                        await peer.SendAnnouncementAsync(announce);
                    }
                    catch (NetworkException ex)
                    {
                        Logger.LogError(ex, $"Error while announcing to {peer}.");
                        await HandleNetworkException(peer, ex);
                    }
                }
                
            }, NetworkConstants.AnnouncementBroadcastQueueName);
            
            return Task.CompletedTask;
        }
        
        public Task BroadcastTransactionAsync(Transaction transaction)
        {
            var beforeEnqueue = TimestampHelper.GetUtcNow();
            _taskQueueManager.Enqueue(async () =>
            {
                var execTime = TimestampHelper.GetUtcNow();
                if (execTime > beforeEnqueue +
                    TimestampHelper.DurationFromMilliseconds(NetworkConstants.TransactionQueueJobTimeout))
                {
                    Logger.LogWarning($"Transaction too old: {execTime - beforeEnqueue}");
                    return;
                }
                
                foreach (var peer in _peerPool.GetPeers())
                {
                    try
                    {
                        await peer.SendTransactionAsync(transaction);
                    }
                    catch (NetworkException ex)
                    {
                        Logger.LogError(ex, "Error while sending transaction.");
                        await HandleNetworkException(peer, ex);
                    }
                }
                
            }, NetworkConstants.TransactionBroadcastQueueName);

            return Task.CompletedTask;
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
            IPeer bestPeer = _peerPool.GetPeers().FirstOrDefault(p => p.IsBest);
            
            if (bestPeer == null)
                Logger.LogWarning("No best peer.");
            else if (bestPeer.Info.Pubkey != peerPubKey)
                peers.Add(bestPeer);
            
            Random rnd = new Random();
            
            // Fill with random peers.
            List<IPeer> randomPeers = _peerPool.GetPeers()
                .Where(p => p.Info.Pubkey != peerPubKey && (bestPeer == null || p.Info.Pubkey != bestPeer.Info.Pubkey))
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
            return await RequestAsync(peers, p => p.GetBlockByHashAsync(hash), blockWithTransactions => blockWithTransactions != null, peer);
        }

        private async Task<(IPeer, T)> DoRequest<T>(IPeer peer, Func<IPeer, Task<T>> func) where T : class
        {
            try
            {
                var res = await func(peer);
                
                return (peer, res);
            }
            catch (NetworkException ex)
            {
                Logger.LogError(ex, $"Error while requesting block(s) from {peer.IpAddress}.");
                await HandleNetworkException(peer, ex);
            }
            
            return (peer, null);
        }

        private async Task HandleNetworkException(IPeer peer, NetworkException exception)
        {
            if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
            {
                await _networkServer.DisconnectAsync(peer);
            }
            else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
            {
                Logger.LogError($"Queuing peer for reconnection {peer.IpAddress}.");
                QueueNetworkTask(async () => await RecoverPeerAsync(peer));
            }
        }
        
        private async Task RecoverPeerAsync(IPeer peer)
        {
            if (peer.IsReady) // peer recovered already
                return;
                
            var success = await peer.TryRecoverAsync();

            if (!success)
                await _networkServer.DisconnectAsync(peer);
        }
        
        private void QueueNetworkTask(Func<Task> task)
        {
            _taskQueueManager.Enqueue(task, NetworkConstants.PeerReconnectionQueueName);
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
                Logger.LogDebug("No peer succeeded.");
                return null;
            }

            IPeer taskPeer = finished.Result.Item1;
            T taskRes = finished.Result.Item2;
            
            UpdateBestPeer(taskPeer);
            
            if (suggested != taskPeer.Info.Pubkey)
                Logger.LogWarning($"Suggested {suggested}, used {taskPeer.Info.Pubkey}");
            
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
    }
}