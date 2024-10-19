using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.Types;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc;

public interface IGrpcRequestProcessor
{
    Task ProcessBlockAsync(BlockWithTransactions block, string peerPubkey);
    Task ProcessAnnouncementAsync(BlockAnnouncement announcement, string peerPubkey);
    Task ProcessTransactionAsync(Transaction tx, string peerPubkey);
    Task ProcessLibAnnouncementAsync(LibAnnouncement announcement, string peerPubkey);
    Task<NodeList> GetNodesAsync(NodesRequest request, string peerInfo);
    Task<BlockReply> GetBlockAsync(BlockRequest request, string peerInfo, string peerPubkey, string requestId = null);
    Task<BlockList> GetBlocksAsync(BlocksRequest request, string peerInfo, string requestId = null);
    Task<VoidReply> ConfirmHandshakeAsync(string peerInfo, string peerPubkey, string requestId = null);
    Task<VoidReply> DisconnectAsync(DisconnectReason request, string peerInfo, string peerPubkey, string requestId = null);
}

public partial class GrpcRequestProcessor : IGrpcRequestProcessor, ISingletonDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly IConnectionService _connectionService;
    private readonly ISyncStateService _syncStateService;
    private readonly INodeManager _nodeManager;

    public GrpcRequestProcessor(IBlockchainService blockchainService, IConnectionService connectionService, ISyncStateService syncStateService, INodeManager nodeManager)
    {
        _blockchainService = blockchainService;
        _connectionService = connectionService;
        _syncStateService = syncStateService;
        _nodeManager = nodeManager;
        EventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<GrpcRequestProcessor>.Instance;
    }

    public ILocalEventBus EventBus { get; set; }
    public ILogger<GrpcRequestProcessor> Logger { get; set; }

    public Task ProcessBlockAsync(BlockWithTransactions block, string peerPubkey)
    {
        var peer = TryGetPeerByPubkey(peerPubkey);

        if (peer.SyncState != SyncState.Finished) peer.SyncState = SyncState.Finished;

        if (!peer.TryAddKnownBlock(block.GetHash()))
            return Task.CompletedTask;
        _ = EventBus.PublishAsync(new BlockReceivedEvent(block, peerPubkey));
        return Task.CompletedTask;
    }

    public Task ProcessAnnouncementAsync(BlockAnnouncement announcement, string peerPubkey)
    {
        if (announcement?.BlockHash == null)
        {
            Logger.LogWarning($"Received null announcement or header from {peerPubkey}.");
            return Task.CompletedTask;
        }

        var peer = TryGetPeerByPubkey(peerPubkey);

        if (!peer.TryAddKnownBlock(announcement.BlockHash))
            return Task.CompletedTask;

        if (peer.SyncState != SyncState.Finished) peer.SyncState = SyncState.Finished;

        _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(announcement, peerPubkey));

        return Task.CompletedTask;
    }

    public async Task ProcessTransactionAsync(Transaction tx, string peerPubkey)
    {
        var chain = await _blockchainService.GetChainAsync();

        // if this transaction's ref block is a lot higher than our chain 
        // then don't participate in p2p network
        if (tx.RefBlockNumber > chain.LongestChainHeight + NetworkConstants.DefaultInitialSyncOffset)
            return;

        var peer = TryGetPeerByPubkey(peerPubkey);

        if (!peer.TryAddKnownTransaction(tx.GetHash()))
            return;

        _ = EventBus.PublishAsync(new TransactionsReceivedEvent { Transactions = new List<Transaction> { tx } });
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

        if (peer.SyncState != SyncState.Finished) peer.SyncState = SyncState.Finished;

        return Task.CompletedTask;
    }


    /// <summary>
    ///     This method returns a block. The parameter is a <see cref="BlockRequest" /> object, if the value
    ///     of <see cref="BlockRequest.Hash" /> is not null, the request is by ID, otherwise it will be
    ///     by height.
    /// </summary>
    [ExceptionHandler(typeof(Exception), TargetType = typeof(GrpcRequestProcessor),
        MethodName = nameof(HandleExceptionWhileGettingBlock))]
    public async Task<BlockReply> GetBlockAsync(BlockRequest request, string peerInfo, string peerPubkey,
        string requestId)
    {
        if (request == null || request.Hash == null || _syncStateService.SyncState != SyncState.Finished)
            return new BlockReply();

        Logger.LogDebug($"Peer {peerInfo} requested block {request.Hash}.");

        var block = await _blockchainService.GetBlockWithTransactionsByHashAsync(request.Hash);

        if (block == null)
        {
            Logger.LogDebug($"Could not find block {request.Hash} for {peerInfo}.");
        }
        else
        {
            var peer = _connectionService.GetPeerByPubkey(peerPubkey);
            peer.TryAddKnownBlock(block.GetHash());
        }

        return new BlockReply { Block = block };
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(GrpcRequestProcessor),
        MethodName = nameof(HandleExceptionWhileGettingBlocks))]
    public async Task<BlockList> GetBlocksAsync(BlocksRequest request, string peerInfo, string requestId)
    {
        if (request == null ||
            request.PreviousBlockHash == null ||
            _syncStateService.SyncState != SyncState.Finished ||
            request.Count == 0 ||
            request.Count > GrpcConstants.MaxSendBlockCountLimit)
            return new BlockList();

        Logger.LogDebug(
            "Peer {peerInfo} requested {count} blocks from {preHash}. requestId={requestId}", peerInfo, request.Count,
            request.PreviousBlockHash.ToHex(), requestId);

        var blockList = new BlockList();

        var blocks =
            await _blockchainService.GetBlocksWithTransactionsAsync(request.PreviousBlockHash, request.Count);

        blockList.Blocks.AddRange(blocks);
        Logger.LogDebug(
            "Replied to {peerInfo} with {count}, request was {request}. requestId={requestId}", peerInfo,
            blockList.Blocks.Count, request, requestId);

        return blockList;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(GrpcRequestProcessor),
        MethodName = nameof(HandleExceptionWhileGettingNodes))]
    public async Task<NodeList> GetNodesAsync(NodesRequest request, string peerInfo)
    {
        if (request == null)
            return new NodeList();

        var nodesCount = Math.Min(request.MaxCount, GrpcConstants.DefaultDiscoveryMaxNodesToResponse);
        Logger.LogDebug("Peer {peerInfo} requested {nodesCount} nodes.", peerInfo, nodesCount);

        var nodes = await _nodeManager.GetRandomNodesAsync(nodesCount);

        Logger.LogDebug("Sending {Count} to {peerInfo}.", nodes.Nodes.Count, peerInfo);
        return nodes;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(GrpcRequestProcessor),
        MethodName = nameof(HandleExceptionWhileConfirmingHandshake))]
    public Task<VoidReply> ConfirmHandshakeAsync(string peerInfo, string peerPubkey, string requestId)
    {
        Logger.LogDebug("Peer {peerInfo} has requested a handshake confirmation. requestId={requestId}", peerInfo,
            requestId);
        _connectionService.ConfirmHandshake(peerPubkey);
        return Task.FromResult(new VoidReply());
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(GrpcRequestProcessor),
        MethodName = nameof(HandleExceptionWhileRemovingPeer))]
    public async Task<VoidReply> DisconnectAsync(DisconnectReason request, string peerInfo, string peerPubkey, string requestId)
    {
        Logger.LogDebug("Peer {peerInfo} has sent a disconnect request. reason={reason} requestId={requestId}", peerInfo, request, requestId);
        await _connectionService.RemovePeerAsync(peerPubkey);
        return new VoidReply();
    }

    /// <summary>
    ///     Try to get the peer based on peerPubkey.
    /// </summary>
    /// <param name="peerPubkey"></param>
    /// <returns></returns>
    /// <exception cref="RpcException">
    ///     If the peer does not exist, a cancelled RPC exception is thrown to tell the client.
    ///     Need to verify the existence of the peer here,
    ///     because when we start transferring data using the streaming RPC,
    ///     the request no longer goes through the <see cref="AuthInterceptor" />.
    /// </exception>
    private GrpcPeer TryGetPeerByPubkey(string peerPubkey)
    {
        var peer = _connectionService.GetPeerByPubkey(peerPubkey);

        if (peer != null)
            return peer;

        Logger.LogDebug("Peer: {peerPubkey} already removed.", peerPubkey);
        throw new RpcException(Status.DefaultCancelled);
    }
}