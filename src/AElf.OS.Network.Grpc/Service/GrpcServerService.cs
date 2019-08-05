using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Infrastructure;
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
        private readonly IPeerDiscoveryService _peerDiscoveryService;
        private readonly IConnectionService _connectionService;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcServerService> Logger { get; set; }

        public GrpcServerService(ISyncStateService syncStateService, IConnectionService connectionService,
            IBlockchainService blockchainService, IPeerDiscoveryService peerDiscoveryService)
        {
            _syncStateService = syncStateService;
            _connectionService = connectionService;
            _blockchainService = blockchainService;
            _peerDiscoveryService = peerDiscoveryService;

            EventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<GrpcServerService>.Instance;
        }

        /// <summary>
        /// First step of the connect/auth process. Used to initiate a connection. The provided payload should be the
        /// clients authentication information. When receiving this call, protocol dictates you send the client your auth
        /// information. The response says whether or not you can connect.
        /// </summary>
        public override async Task<ConnectReply> Connect(ConnectRequest connectionRequest, ServerCallContext context)
        {
            Logger.LogTrace($"{context.Peer} has initiated a connection.");
            return await _connectionService.DialBackAsync(context.Peer, connectionRequest.Info);
        }
        
        public override async Task<HandshakeReply> DoHandshake(HandshakeRequest request, ServerCallContext context)
        {
            Logger.LogDebug($"Peer {context.GetPeerInfo()} has requested a handshake.");
            return await _connectionService.CheckIncomingHandshakeAsync(context.GetPublicKey(), request.Handshake);
        }
        
        public override async Task<VoidReply> BlockBroadcastStream(IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(r =>
            {
                _ = EventBus.PublishAsync(new BlockReceivedEvent(r,context.GetPublicKey()));
                return Task.CompletedTask;
            });
            
            return new VoidReply();
        }

        public override async Task<VoidReply> AnnouncementBroadcastStream(IAsyncStreamReader<BlockAnnouncement> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(async r => await ProcessAnnouncement(r, context));
            return new VoidReply();
        }

        public Task ProcessAnnouncement(BlockAnnouncement announcement, ServerCallContext context)
        {
            if (announcement?.BlockHash == null)
            {
                Logger.LogError($"Received null announcement or header from {context.GetPeerInfo()}.");
                return Task.CompletedTask;
            }
            
            Logger.LogDebug($"Received announce {announcement.BlockHash} from {context.GetPeerInfo()}.");

            var peer = _connectionService.GetPeerByPubkey(context.GetPublicKey());
            peer?.AddKnowBlock(announcement);

            _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(announcement, context.GetPublicKey()));
            
            return Task.CompletedTask;
        }
        
        public override async Task<VoidReply> TransactionBroadcastStream(IAsyncStreamReader<Transaction> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(async tx => await ProcessTransaction(tx, context));
            return new VoidReply();
        }

        /// <summary>
        /// This method is called when another peer broadcasts a transaction.
        /// </summary>
        public override async Task<VoidReply> SendTransaction(Transaction tx, ServerCallContext context)
        {
            await ProcessTransaction(tx, context);
            return new VoidReply();
        }

        private async Task ProcessTransaction(Transaction tx, ServerCallContext context)
        {
            var chain = await _blockchainService.GetChainAsync();
            
            // if this transaction's ref block is a lot higher than our chain 
            // then don't participate in p2p network
            if (tx.RefBlockNumber > chain.LongestChainHeight + NetworkConstants.DefaultInitialSyncOffset)
                return;
            
            _ = EventBus.PublishAsync(new TransactionsReceivedEvent { Transactions = new List<Transaction> {tx} });
        }

        /// <summary>
        /// This method is called when a peer wants to broadcast an announcement.
        /// </summary>
        public override async Task<VoidReply> SendAnnouncement(BlockAnnouncement an, ServerCallContext context)
        {
            await ProcessAnnouncement(an, context);
            return new VoidReply();
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

            var block = await _blockchainService.GetBlockWithTransactionsByHash(request.Hash);
            
            if (block == null)
                Logger.LogDebug($"Could not find block {request.Hash} for {context.GetPeerInfo()}.");

            return new BlockReply { Block = block };
        }

        public override async Task<BlockList> RequestBlocks(BlocksRequest request, ServerCallContext context)
        {
            if (request == null || request.PreviousBlockHash == null || _syncStateService.SyncState != SyncState.Finished)
                return new BlockList();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested {request.Count} blocks from {request.PreviousBlockHash}.");

            var blockList = new BlockList();
            
            var blocks = await _blockchainService.GetBlocksWithTransactions(request.PreviousBlockHash, request.Count);

            if (blocks == null)
                return blockList;
            
            blockList.Blocks.AddRange(blocks);

            if (blockList.Blocks.Count != request.Count)
                Logger.LogTrace($"Replied with {blockList.Blocks.Count} blocks for request {request}");

            if (NetworkOptions.CompressBlocksOnRequest)
            {
                var headers = new Metadata{new Metadata.Entry(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst)};
                await context.WriteResponseHeadersAsync(headers);
            }
            
            return blockList;
        }

        public override async Task<NodeList> GetNodes(NodesRequest request, ServerCallContext context)
        {
            if (request == null)
                return new NodeList();
            
            Logger.LogDebug($"Peer {context.GetPeerInfo()} requested {request.MaxCount} nodes.");
            
            var nodes = await _peerDiscoveryService.GetNodesAsync(request.MaxCount);
            
            Logger.LogDebug($"Sending {nodes.Nodes.Count} to {context.GetPeerInfo()}.");

            return nodes;
        }

        public override Task<PongReply> Ping(PingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PongReply());
        }

        /// <summary>
        /// Clients should call this method to disconnect explicitly.
        /// </summary>
        public override Task<VoidReply> Disconnect(DisconnectReason request, ServerCallContext context)
        {
            Logger.LogDebug($"Peer {context.GetPeerInfo()} has sent a disconnect request.");
            _connectionService.RemovePeer(context.GetPublicKey());
            return Task.FromResult(new VoidReply());
        }
    }
}