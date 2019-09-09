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
        private readonly IKnownBlockCacheProvider _knownBlockCacheProvider;
        private readonly IBroadcastPrivilegedPubkeyListProvider _broadcastPrivilegedPubkeyListProvider;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool, ITaskQueueManager taskQueueManager, IAElfNetworkServer networkServer,
            IKnownBlockCacheProvider knownBlockCacheProvider,
            IBroadcastPrivilegedPubkeyListProvider broadcastPrivilegedPubkeyListProvider)
        {
            _peerPool = peerPool;
            _taskQueueManager = taskQueueManager;
            _networkServer = networkServer;
            _knownBlockCacheProvider = knownBlockCacheProvider;
            _broadcastPrivilegedPubkeyListProvider = broadcastPrivilegedPubkeyListProvider;

            Logger = NullLogger<NetworkService>.Instance;
        }

        public async Task<bool> AddPeerAsync(string address)
        {
            if (IpEndpointHelper.TryParse(address, out IPEndPoint endpoint))
                return await _networkServer.ConnectAsync(endpoint);

            return false;
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            if (!IpEndpointHelper.TryParse(address, out IPEndPoint endpoint)) 
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

        public List<PeerInfo> GetPeers()
        {   
            return _peerPool.GetPeers(true).Select(PeerInfoHelper.FromNetworkPeer).ToList();
        }

        public PeerInfo GetPeerByPubkey(string peerPubkey)
        {
            var peer = _peerPool.FindPeerByPublicKey(peerPubkey);
            return peer == null ? null : PeerInfoHelper.FromNetworkPeer(peer);
        }
        
        public async Task<bool> RemovePeerByPubkeyAsync(string peerPubKey)
        {
            var peer = _peerPool.FindPeerByPublicKey(peerPubKey);
            if (peer == null)
            {
                Logger.LogWarning($"Could not find peer: {peerPubKey}");
                return false;
            }
            
            await _networkServer.DisconnectAsync(peer);

            return true;
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

        public async Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
        {
            if (!TryAddKnownBlock(blockWithTransactions.Header))
                return;

            if (IsOldBlock(blockWithTransactions.Header))
                return;
            
            var nextMinerPubkey = await GetNextMinerPubkey(blockWithTransactions.Header);
            
            var nextPeer = _peerPool.FindPeerByPublicKey(nextMinerPubkey);
            if (nextPeer != null)
                await SendBlockAsync(nextPeer, blockWithTransactions);

            foreach (var peer in _peerPool.GetPeers())
            {
                if (nextPeer != null && peer.Info.Pubkey == nextPeer.Info.Pubkey)
                    continue;

                await SendBlockAsync(peer, blockWithTransactions);
            }
        }
        
        private async Task SendBlockAsync(IPeer peer, BlockWithTransactions blockWithTransactions)
        {
            try
            {
                peer.EnqueueBlock(blockWithTransactions, async ex =>
                {
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
            
            if (!TryAddKnownBlock(blockHeader))
                return Task.CompletedTask;
            
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
                    peer.EnqueueAnnouncement(blockAnnouncement, async ex =>
                    {
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
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    peer.EnqueueTransaction(transaction, async ex =>
                    {
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

        public async Task<Response<List<BlockWithTransactions>>> GetBlocksAsync(Hash previousBlock, int count, 
            string peerPubkey)
        {
            IPeer peer = _peerPool.FindPeerByPublicKey(peerPubkey);
            
            if (peer == null)
                throw new InvalidOperationException($"Could not find peer {peerPubkey}.");

            var response = await Request(peer, p => p.GetBlocksAsync(previousBlock, count));

            if (response != null && response.Success && (response.Payload.Count == 0 || response.Payload.Count != count))
                Logger.LogWarning($"Block count miss match, asked for {count} but got {response.Payload.Count}");

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

        private async Task<Response<T>> Request<T>(IPeer peer, Func<IPeer, Task<T>> func) where T : class
        {
            try
            {
                return new Response<T>(await func(peer));
            }
            catch (NetworkException ex)
            {
                Logger.LogError(ex, $"Error while requesting block(s) from {peer.RemoteEndpoint}.");
                await HandleNetworkException(peer, ex);
            }

            return new Response<T>();
        }

        private async Task HandleNetworkException(IPeer peer, NetworkException exception)
        {
            if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
            {
                Logger.LogError(exception, $"Removing unrecoverable {peer}.");
                await _networkServer.DisconnectAsync(peer);
            }
            else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
            {
                Logger.LogError(exception, $"Queuing peer for reconnection {peer.IpAddress}.");
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
    }
}