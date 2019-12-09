using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Types;
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
        private readonly IBroadcastPrivilegedPubkeyListProvider _broadcastPrivilegedPubkeyListProvider;
        private readonly IBlackListedPeerProvider _blackListedPeerProvider;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool, ITaskQueueManager taskQueueManager, IAElfNetworkServer networkServer,
            IBlackListedPeerProvider blackListedPeerProvider, 
            IBroadcastPrivilegedPubkeyListProvider broadcastPrivilegedPubkeyListProvider)
        {
            _peerPool = peerPool;
            _taskQueueManager = taskQueueManager;
            _networkServer = networkServer;
            _broadcastPrivilegedPubkeyListProvider = broadcastPrivilegedPubkeyListProvider;
            _blackListedPeerProvider = blackListedPeerProvider;

            Logger = NullLogger<NetworkService>.Instance;
        }

        public async Task<bool> AddPeerAsync(string address)
        {
            if (AElfPeerEndpointHelper.TryParse(address, out DnsEndPoint endpoint))
                return await _networkServer.ConnectAsync(endpoint);
            
            Logger.LogWarning($"Could not parse endpoint {address}.");

            return false;
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            if (!AElfPeerEndpointHelper.TryParse(address, out DnsEndPoint endpoint)) 
                return false;
            
            var peer = _peerPool.FindPeerByEndpoint(endpoint);
            if (peer == null)
            {
                Logger.LogWarning($"Could not find peer at address {address}");
                return false;
            }

            await _networkServer.DisconnectAsync(peer);

            return true;
        }
        
        public async Task<bool> RemovePeerByPubkeyAsync(string peerPubKey, bool blacklistPeer = false)
        {
            var peer = _peerPool.FindPeerByPublicKey(peerPubKey);
            if (peer == null)
            {
                Logger.LogWarning($"Could not find peer: {peerPubKey}");
                return false;
            }
            
            if (blacklistPeer)
            {
                _blackListedPeerProvider.AddHostToBlackList(peer.RemoteEndpoint.Host);
                Logger.LogDebug($"Blacklisted {peer.RemoteEndpoint.Host} ({peerPubKey})");
            }
            
            await _networkServer.DisconnectAsync(peer);

            return true;
        }

        public List<PeerInfo> GetPeers(bool includeFailing = true)
        {   
            return _peerPool.GetPeers(includeFailing).Select(PeerInfoHelper.FromNetworkPeer).ToList();
        }

        public PeerInfo GetPeerByPubkey(string peerPubkey)
        {
            var peer = _peerPool.FindPeerByPublicKey(peerPubkey);
            return peer == null ? null : PeerInfoHelper.FromNetworkPeer(peer);
        }

        private bool IsOldBlock(BlockHeader header)
        {
            var limit = TimestampHelper.GetUtcNow() 
                        - TimestampHelper.DurationFromMinutes(NetworkConstants.DefaultMaxBlockAgeToBroadcastInMinutes);
            
            if (header.Time < limit)
                return true;

            return false;
        }

        public async Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
        {
            if (IsOldBlock(blockWithTransactions.Header))
                return;
            
            var nextMinerPubkey = await GetNextMinerPubkey(blockWithTransactions.Header);
            
            var nextPeer = _peerPool.FindPeerByPublicKey(nextMinerPubkey);
            if (nextPeer != null)
                EnqueueBlock(nextPeer, blockWithTransactions);

            foreach (var peer in _peerPool.GetPeers())
            {
                if (nextPeer != null && peer.Info.Pubkey == nextPeer.Info.Pubkey)
                    continue;

                EnqueueBlock(peer, blockWithTransactions);
            }
        }
        
        private void EnqueueBlock(IPeer peer, BlockWithTransactions blockWithTransactions)
        {
            try
            {
                var blockHash = blockWithTransactions.GetHash();

                if (peer.KnowsBlock(blockHash))
                    return; // block already known to this peer

                peer.EnqueueBlock(blockWithTransactions, async ex =>
                {
                    peer.TryAddKnownBlock(blockHash);

                    if (ex != null)
                    {
                        Logger.LogError(ex, $"Error while broadcasting block to {peer}.");
                        await HandleNetworkException(peer, ex);
                    }
                });
            }
            catch (NetworkException ex)
            {
                Logger.LogError(ex, $"Error while broadcasting block to {peer}.");
            }
        }
        
        private async Task<string> GetNextMinerPubkey(BlockHeader blockHeader)
        {
            var broadcastList = await _broadcastPrivilegedPubkeyListProvider.GetPubkeyList(blockHeader);
            return broadcastList.IsNullOrEmpty() ? null : broadcastList[0];
        }

        public Task BroadcastAnnounceAsync(BlockHeader blockHeader, bool hasFork)
        {
            var blockHash = blockHeader.GetHash();

            if (IsOldBlock(blockHeader))
                return Task.CompletedTask;
            
            var blockAnnouncement = new BlockAnnouncement
            {
                BlockHash = blockHash,
                BlockHeight = blockHeader.Height,
                HasFork = hasFork
            };

            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    if (peer.KnowsBlock(blockHash))
                        return Task.CompletedTask; // block already known to this peer

                    peer.EnqueueAnnouncement(blockAnnouncement, async ex =>
                    {
                        peer.TryAddKnownBlock(blockHash);
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"Error while broadcasting announcement to {peer}.");
                            await HandleNetworkException(peer, ex);
                        }
                    });
                }
                catch (NetworkException ex)
                {
                    Logger.LogError(ex, $"Error while broadcasting announcement to {peer}.");
                }
            }
            
            return Task.CompletedTask;
        }
        
        public Task BroadcastTransactionAsync(Transaction transaction)
        {
            var txHash = transaction.GetHash();
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    if (peer.KnowsTransaction(txHash)) 
                        return Task.CompletedTask; // transaction already known to this peer
                    
                    peer.EnqueueTransaction(transaction, async ex =>
                    {
                        peer.TryAddKnownTransaction(txHash);
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"Error while broadcasting transaction to {peer}.");
                            await HandleNetworkException(peer, ex);
                        }
                    });
                }
                catch (NetworkException ex)
                {
                    Logger.LogError(ex, $"Error while broadcasting transaction to {peer}.");
                }
            }
            
            return Task.CompletedTask;
        }
        
        public Task BroadcastLibAnnounceAsync(Hash libHash, long libHeight)
        {
            var announce = new LibAnnouncement
            {
                LibHash = libHash,
                LibHeight = libHeight
            };

            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    peer.EnqueueLibAnnouncement(announce, async ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"Error while broadcasting lib announcement to {peer}.");
                            await HandleNetworkException(peer, ex);
                        }
                    });
                }
                catch (NetworkException ex)
                {
                    Logger.LogError(ex, $"Error while broadcasting lib announcement to {peer}.");
                }
            }
            
            return Task.CompletedTask;
        }

        public async Task SendHealthChecksAsync()
        {
            foreach (var peer in _peerPool.GetPeers())
            {
                Logger.LogDebug($"Health checking: {peer}");
                
                try
                {
                    await peer.CheckHealthAsync();
                }
                catch (NetworkException ex)
                {
                    if (ex.ExceptionType == NetworkExceptionType.Unrecoverable)
                    {
                        Logger.LogError(ex, $"Removing unhealthy peer {peer}.");
                        await _networkServer.TrySchedulePeerReconnectionAsync(peer);
                    }
                }
            }
        }

        public void CheckNtpDrift()
        {
            _networkServer.CheckNtpDrift();
        }

        public async Task<Response<List<BlockWithTransactions>>> GetBlocksAsync(Hash previousBlock, int count, 
            string peerPubkey)
        {
            IPeer peer = _peerPool.FindPeerByPublicKey(peerPubkey);
            
            if (peer == null)
                throw new InvalidOperationException($"Could not find peer {peerPubkey}.");

            var response = await Request(peer, p => p.GetBlocksAsync(previousBlock, count));

            if (response != null && response.Success && response.Payload != null 
                && (response.Payload.Count == 0 || response.Payload.Count != count))
                Logger.LogWarning($"Requested blocks from {peer} - count miss match, asked for {count} but got {response.Payload.Count} (from {previousBlock})");

            return response;
        }
        
        public async Task<Response<BlockWithTransactions>> GetBlockByHashAsync(Hash hash, string peerPubkey)
        {
            IPeer peer = _peerPool.FindPeerByPublicKey(peerPubkey);
            
            if (peer == null)
                throw new InvalidOperationException($"Could not find peer {peerPubkey}.");
            
            Logger.LogDebug($"Getting block by hash, hash: {hash} from {peer}.");

            return await Request(peer, p => p.GetBlockByHashAsync(hash));
        }
        
        public bool IsPeerPoolFull()
        {
            return _peerPool.IsFull();
        }

        private async Task<Response<T>> Request<T>(IPeer peer, Func<IPeer, Task<T>> func) where T : class
        {
            try
            {
                return new Response<T>(await func(peer));
            }
            catch (NetworkException ex)
            {
                Logger.LogError(ex, $"Error while requesting block(s) from {peer.RemoteEndpoint}.");
                
                if (ex.ExceptionType == NetworkExceptionType.HandlerException)
                    return new Response<T>(default(T));
                
                await HandleNetworkException(peer, ex);
            }

            return new Response<T>();
        }

        private async Task HandleNetworkException(IPeer peer, NetworkException exception)
        {
            if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
            {
                Logger.LogError(exception, $"Removing unrecoverable {peer}.");
                await _networkServer.TrySchedulePeerReconnectionAsync(peer);
            }
            else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
            {
                Logger.LogError(exception, $"Queuing peer for reconnection {peer.RemoteEndpoint}.");
                QueueNetworkTask(async () => await RecoverPeerAsync(peer));
            }
        }

        private async Task RecoverPeerAsync(IPeer peer)
        {
            if (peer.IsReady) // peer recovered already
                return;
                
            var success = await peer.TryRecoverAsync();

            if (!success)
                await _networkServer.TrySchedulePeerReconnectionAsync(peer);
        }
        
        private void QueueNetworkTask(Func<Task> task)
        {
            _taskQueueManager.Enqueue(task, NetworkConstants.PeerReconnectionQueueName);
        }
    }
}