using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Grpc.Helpers;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Implementation of the grpc generated service. It contains the rpc methods
    /// exposed to peers.
    /// </summary>
    public class GrpcServerService : PeerService.PeerServiceBase
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly ISyncStateService _syncStateService;
        private readonly IBlockchainService _blockchainService;
        private readonly INodeManager _nodeManager;
        private readonly IConnectionService _connectionService;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcServerService> Logger { get; set; }

        public GrpcServerService(ISyncStateService syncStateService, IConnectionService connectionService,
            IBlockchainService blockchainService, INodeManager nodeManager)
        {
            _syncStateService = syncStateService;
            _connectionService = connectionService;
            _blockchainService = blockchainService;
            _nodeManager = nodeManager;

            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<GrpcServerService>.Instance;
        }

        public override async Task<HandshakeReply> DoHandshake(HandshakeRequest request, ServerCallContext context)
        {
            try
            {
                Logger.LogDebug($"Peer {context.Peer} has requested a handshake.");

                if (context.AuthContext?.Properties != null)
                {
                    foreach (var authProperty in context.AuthContext.Properties)
                        Logger.LogDebug($"Auth property: {authProperty.Name} -> {authProperty.Value}");
                }

                if(!GrpcEndPointHelper.ParseDnsEndPoint(context.Peer, out DnsEndPoint peerEndpoint))
                    return new HandshakeReply { Error = HandshakeError.InvalidConnection};
            
                return await _connectionService.DoHandshakeAsync(peerEndpoint, request.Handshake);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"Handshake failed - {context.Peer}: ");
                throw;
            }
        }

        public override Task<VoidReply> ConfirmHandshake(ConfirmHandshakeRequest request,
            ServerCallContext context)
        {
            try
            {
                Logger.LogDebug($"Peer {context.GetPeerInfo()} has requested a handshake confirmation.");

                _connectionService.ConfirmHandshake(context.GetPublicKey());
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"Confirm handshake error - {context.GetPeerInfo()}: ");
                throw;
            }

            return Task.FromResult(new VoidReply());
        }

        public override async Task<VoidReply> BlockBroadcastStream(
            IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
        {
            Logger.LogDebug($"Block stream started with {context.GetPeerInfo()} - {context.Peer}.");

            try
            {
                var peerPubkey = context.GetPublicKey();
                await requestStream.ForEachAsync(async block => await ProcessBlockAsync(block, peerPubkey));
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Block stream error - {context.GetPeerInfo()}: ");
                throw;
            }

            Logger.LogDebug($"Block stream finished with {context.GetPeerInfo()} - {context.Peer}.");

            return new VoidReply();
        }
        
        private Task ProcessBlockAsync(BlockWithTransactions block, string peerPubkey)
        {
            var peer = TryGetPeerByPubkey(peerPubkey);

            if (peer.SyncState != SyncState.Finished)
            {
                peer.SyncState = SyncState.Finished;
            }
            
            if (!peer.TryAddKnownBlock(block.GetHash()))
                return Task.CompletedTask;
                        
            _ = EventBus.PublishAsync(new BlockReceivedEvent(block, peerPubkey));
            return Task.CompletedTask;
        }

        public override async Task<VoidReply> AnnouncementBroadcastStream(
            IAsyncStreamReader<BlockAnnouncement> requestStream, ServerCallContext context)
        {
            Logger.LogDebug($"Announcement stream started with {context.GetPeerInfo()} - {context.Peer}.");

            try
            {
                var peerPubkey = context.GetPublicKey();
                await requestStream.ForEachAsync(async r => await ProcessAnnouncementAsync(r, peerPubkey));
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Announcement stream error: {context.GetPeerInfo()}");
                throw;
            }

            Logger.LogDebug($"Announcement stream finished with {context.GetPeerInfo()} - {context.Peer}.");

            return new VoidReply();
        }

        private Task ProcessAnnouncementAsync(BlockAnnouncement announcement, string peerPubkey)
        {
            if (announcement?.BlockHash == null)
            {
                Logger.LogWarning($"Received null announcement or header from {peerPubkey}.");
                return Task.CompletedTask;
            }

            var peer = TryGetPeerByPubkey(peerPubkey);

            if (!peer.TryAddKnownBlock(announcement.BlockHash))
                return Task.CompletedTask;

            if (peer.SyncState != SyncState.Finished)
            {
                peer.SyncState = SyncState.Finished;
            }

            _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(announcement, peerPubkey));

            return Task.CompletedTask;
        }

        public override async Task<VoidReply> TransactionBroadcastStream(IAsyncStreamReader<Transaction> requestStream,
            ServerCallContext context)
        {
            Logger.LogDebug($"Transaction stream started with {context.GetPeerInfo()} - {context.Peer}.");

            try
            {
                var peerPubkey = context.GetPublicKey();
                await requestStream.ForEachAsync(async tx => await ProcessTransactionAsync(tx, peerPubkey));
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Transaction stream error - {context.GetPeerInfo()}: ");
                throw;
            }

            Logger.LogDebug($"Transaction stream finished with {context.GetPeerInfo()} - {context.Peer}.");

            return new VoidReply();
        }

        private async Task ProcessTransactionAsync(Transaction tx, string peerPubkey)
        {
            var chain = await _blockchainService.GetChainAsync();

            // if this transaction's ref block is a lot higher than our chain 
            // then don't participate in p2p network
            if (tx.RefBlockNumber > chain.LongestChainHeight + NetworkConstants.DefaultInitialSyncOffset)
                return;

            var peer = TryGetPeerByPubkey(peerPubkey);

            if (!peer.TryAddKnownTransaction(tx.GetHash()))
                return;
            
            _ = EventBus.PublishAsync(new TransactionsReceivedEvent {Transactions = new List<Transaction> {tx}});
        }

        public override async Task<VoidReply> LibAnnouncementBroadcastStream(
            IAsyncStreamReader<LibAnnouncement> requestStream, ServerCallContext context)
        {
            Logger.LogDebug($"Lib announcement stream started with {context.GetPeerInfo()} - {context.Peer}.");

            try
            {
                var peerPubkey = context.GetPublicKey();
                await requestStream.ForEachAsync(async r => await ProcessLibAnnouncementAsync(r, peerPubkey));
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Lib announcement stream error: {context.GetPeerInfo()}");
                throw;
            }

            Logger.LogDebug($"Lib announcement stream finished with {context.GetPeerInfo()} - {context.Peer}.");

            return new VoidReply();
        }

        public Task ProcessLibAnnouncementAsync(LibAnnouncement announcement, string peerPubkey)
        {
            if (announcement?.LibHash == null)
            {
                Logger.LogWarning($"Received null or empty announcement from {peerPubkey}.");
                return Task.CompletedTask;
            }

            Logger.LogDebug(
                $"Received lib announce hash: {announcement.LibHash}, height {announcement.LibHeight} from {peerPubkey}.");

            var peer = TryGetPeerByPubkey(peerPubkey);

            peer.UpdateLastKnownLib(announcement);

            if (peer.SyncState != SyncState.Finished)
            {
                peer.SyncState = SyncState.Finished;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// This method returns a block. The parameter is a <see cref="BlockRequest"/> object, if the value
        /// of <see cref="BlockRequest.Hash"/> is not null, the request is by ID, otherwise it will be
        /// by height.
        /// </summary>
        public override async Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
        {
            if (request == null || request.Hash == null || _syncStateService.SyncState != SyncState.Finished)
                return new BlockReply();

            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested block {request.Hash}.");

            BlockWithTransactions block;
            try
            {
                block = await _blockchainService.GetBlockWithTransactionsByHashAsync(request.Hash);

                if (block == null)
                {
                    Logger.LogDebug($"Could not find block {request.Hash} for {context.GetPeerInfo()}.");
                }
                else
                {
                    var peer = _connectionService.GetPeerByPubkey(context.GetPublicKey());
                    peer.TryAddKnownBlock(block.GetHash());
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"Request block error: {context.GetPeerInfo()}");
                throw;
            }

            return new BlockReply {Block = block};
        }

        public override async Task<BlockList> RequestBlocks(BlocksRequest request, ServerCallContext context)
        {
            if (request == null ||
                request.PreviousBlockHash == null ||
                _syncStateService.SyncState != SyncState.Finished ||
                request.Count == 0 ||
                request.Count > GrpcConstants.MaxSendBlockCountLimit)
            {
                return new BlockList();
            }

            Logger.LogDebug(
                $"Peer {context.GetPeerInfo()} requested {request.Count} blocks from {request.PreviousBlockHash}.");

            var blockList = new BlockList();

            try
            {
                var blocks =
                    await _blockchainService.GetBlocksWithTransactionsAsync(request.PreviousBlockHash, request.Count);

                blockList.Blocks.AddRange(blocks);

                if (NetworkOptions.CompressBlocksOnRequest)
                {
                    var headers = new Metadata
                        {new Metadata.Entry(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst)};
                    await context.WriteResponseHeadersAsync(headers);
                }

                Logger.LogDebug(
                    $"Replied to {context.GetPeerInfo()} with {blockList.Blocks.Count}, request was {request}");
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"Request blocks error - {context.GetPeerInfo()} - request {request}: ");
                throw;
            }

            return blockList;
        }

        public override async Task<NodeList> GetNodes(NodesRequest request, ServerCallContext context)
        {
            if (request == null)
                return new NodeList();

            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested {request.MaxCount} nodes.");

            NodeList nodes;
            try
            {
                nodes = await _nodeManager.GetRandomNodesAsync(request.MaxCount);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Get nodes error: ");
                throw;
            }

            Logger.LogDebug($"Sending {nodes.Nodes.Count} to {context.GetPeerInfo()}.");

            return nodes;
        }

        public override Task<PongReply> Ping(PingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PongReply());
        }

        public override Task<HealthCheckReply> CheckHealth(HealthCheckRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HealthCheckReply());
        }

        /// <summary>
        /// Clients should call this method to disconnect explicitly.
        /// </summary>
        public override async Task<VoidReply> Disconnect(DisconnectReason request, ServerCallContext context)
        {
            Logger.LogDebug($"Peer {context.GetPeerInfo()} has sent a disconnect request.");

            try
            {
               await _connectionService.RemovePeerAsync(context.GetPublicKey());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Disconnect error: ");
                throw;
            }

            return new VoidReply();
        }

        /// <summary>
        /// Try to get the peer based on pubkey.
        /// </summary>
        /// <param name="peerPubkey"></param>
        /// <returns></returns>
        /// <exception cref="RpcException">
        /// If the peer does not exist, a cancelled RPC exception is thrown to tell the client.
        /// Need to verify the existence of the peer here,
        /// because when we start transferring data using the streaming RPC,
        /// the request no longer goes through the <see cref="AuthInterceptor"/>. 
        /// </exception>
        private GrpcPeer TryGetPeerByPubkey(string peerPubkey)
        {
            var peer = _connectionService.GetPeerByPubkey(peerPubkey);

            if (peer != null)
                return peer;
            
            Logger.LogDebug($"Peer: {peerPubkey} already removed.");
            throw new RpcException(Status.DefaultCancelled);
        }
    }
}