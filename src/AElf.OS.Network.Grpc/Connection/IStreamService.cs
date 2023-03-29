using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Application;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Events;
using AElf.OS.Network.Extensions;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc;

public interface IStreamService
{
    Task ProcessStreamRequest(StreamMessage request, IAsyncStreamWriter<StreamMessage> responseStream, ServerCallContext context);
    Task ProcessStreamReply(ByteString reply, string clientPubKey);
}

public class StreamService : IStreamService, ISingletonDependency
{
    private ILogger<StreamService> Logger { get; }
    private readonly IBlockchainService _blockchainService;
    private readonly IConnectionService _connectionService;
    private readonly INodeManager _nodeManager;
    private readonly IStreamTaskResourcePool _streamTaskResourcePool;
    private readonly ISyncStateService _syncStateService;
    private readonly IPeerPool _peerPool;
    private readonly ITaskQueueManager _taskQueueManager;

    private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
    private IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
    private ILocalEventBus EventBus { get; }

    public StreamService(IConnectionService connectionService, IStreamTaskResourcePool streamTaskResourcePool, INodeManager nodeManager, ISyncStateService syncStateService,
        IBlockchainService blockchainService, IPeerPool peerPool, ITaskQueueManager taskQueueManager)
    {
        Logger = NullLogger<StreamService>.Instance;
        _connectionService = connectionService;
        _streamTaskResourcePool = streamTaskResourcePool;
        _nodeManager = nodeManager;
        _syncStateService = syncStateService;
        _blockchainService = blockchainService;
        _peerPool = peerPool;
        _taskQueueManager = taskQueueManager;
        EventBus = NullLocalEventBus.Instance;
    }

    public async Task ProcessStreamRequest(StreamMessage request, IAsyncStreamWriter<StreamMessage> responseStream, ServerCallContext context)
    {
        Logger.LogInformation("receive {requestId} {streamType}", request.RequestId, request.StreamType);
        try
        {
            switch (request.StreamType)
            {
                case StreamType.HandShakeReply:
                case StreamType.DisconnectReply:
                case StreamType.PongReply:
                case StreamType.BlockBroadcastReply:
                case StreamType.TransactionBroadcastReply:
                case StreamType.AnnouncementBroadcastReply:
                case StreamType.LibAnnouncementBroadcastReply:
                case StreamType.ConfirmHandShakeReply:
                case StreamType.HealthCheckReply:
                case StreamType.RequestBlockReply:
                case StreamType.RequestBlocksReply:
                case StreamType.GetNodesReply:
                    _streamTaskResourcePool.TrySetResult(request.RequestId, request);
                    break;
                case StreamType.HandShake:
                    var handleShakeReply = await ProcessHandShake(request, responseStream, context);
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.HandShakeReply, RequestId = request.RequestId, Body = handleShakeReply.ToByteString() });
                    break;
                case StreamType.GetNodes:
                    var nodeList = await GetNodes(NodesRequest.Parser.ParseFrom(request.Body), context.GetPeerInfo());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.GetNodesReply, RequestId = request.RequestId, Body = nodeList.ToByteString() });
                    break;
                case StreamType.HealthCheck:
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.HealthCheckReply, RequestId = request.RequestId, Body = new HealthCheckReply().ToByteString() });
                    break;
                case StreamType.Ping:
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.PongReply, RequestId = request.RequestId, Body = new PongReply().ToByteString() });
                    break;
                case StreamType.Disconnect:
                    await Disconnect(DisconnectReason.Parser.ParseFrom(request.Body), request.RequestId, context.GetPeerInfo(), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.DisconnectReply, RequestId = request.RequestId, Body = new VoidReply().ToByteString() });
                    break;
                case StreamType.ConfirmHandShake:
                    ConfirmHandshake(request.RequestId, context.GetPeerInfo(), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.ConfirmHandShakeReply, RequestId = request.RequestId, Body = new VoidReply().ToByteString() });
                    break;

                case StreamType.RequestBlock:
                    var block = await RequestBlock(BlockRequest.Parser.ParseFrom(request.Body), request.RequestId, context.GetPeerInfo(), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.RequestBlockReply, RequestId = request.RequestId, Body = block.ToByteString() });
                    break;
                case StreamType.RequestBlocks:
                    var blocks = await RequestBlocks(BlocksRequest.Parser.ParseFrom(request.Body), request.RequestId, context.GetPeerInfo());
                    SetGrpcGzip(context);
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.RequestBlocksReply, RequestId = request.RequestId, Body = blocks.ToByteString() });
                    break;

                case StreamType.BlockBroadcast:
                    await ProcessBlockAsync(BlockWithTransactions.Parser.ParseFrom(request.Body), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.BlockBroadcastReply, RequestId = request.RequestId, Body = new VoidReply().ToByteString() });
                    break;
                case StreamType.AnnouncementBroadcast:
                    await ProcessAnnouncementAsync(BlockAnnouncement.Parser.ParseFrom(request.Body), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.AnnouncementBroadcastReply, RequestId = request.RequestId, Body = new VoidReply().ToByteString() });
                    break;
                case StreamType.TransactionBroadcast:
                    await ProcessTransactionAsync(Transaction.Parser.ParseFrom(request.Body), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.TransactionBroadcastReply, RequestId = request.RequestId, Body = new VoidReply().ToByteString() });
                    break;
                case StreamType.LibAnnouncementBroadcast:
                    await ProcessLibAnnouncementAsync(LibAnnouncement.Parser.ParseFrom(request.Body), context.GetPublicKey());
                    await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.LibAnnouncementBroadcastReply, RequestId = request.RequestId, Body = new VoidReply().ToByteString() });
                    break;
                case StreamType.Unknown:
                default:
                    Logger.LogWarning("unhandled stream request: {requestId} {type}", request.RequestId, request.StreamType);
                    break;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "request failed {requestId} {streamType}", request.RequestId, request.StreamType);
            throw;
        }
    }

    public async Task ProcessStreamReply(ByteString reply, string clientPubKey)
    {
        var message = StreamMessage.Parser.ParseFrom(reply);
        Logger.LogInformation("receive {requestId} {streamType}", message.RequestId, message.StreamType);
        if (await ProcessStreamRequest(message)) return;

        var peer = _peerPool.FindPeerByPublicKey(clientPubKey) as GrpcPeer;
        if (peer == null)
        {
            Logger.LogError("clientPubKey={clientPubKey} not found for {requestId} {streamType}", message.RequestId, message.StreamType);
            return;
        }

        try
        {
            await ProcessStreamRequest(message, (peer.Holder as OutboundPeerHolder)?.GetResponseStream());
            Logger.LogInformation("handle stream call success, clientPubKey={clientPubKey} request={requestId} {streamType}");
        }
        catch (RpcException ex)
        {
            HandleNetworkException(peer, peer.Holder.HandleRpcException(ex, $"Could not broadcast to {this}: "));
            Logger.LogError(ex, "handle stream call failed, clientPubKey={clientPubKey} request={requestId} {streamType}");
            await Task.Delay(GrpcConstants.StreamRecoveryWaitTime);
        }
        catch (Exception ex)
        {
            HandleNetworkException(peer, new NetworkException("Unknown exception during broadcast.", ex));
            Logger.LogError(ex, "handle stream call failed, clientPubKey={clientPubKey} request={requestId} {streamType}");
            throw;
        }
    }

    public async Task HandleNetworkException(IPeer peer, NetworkException exception)
    {
        if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
        {
            Logger.LogInformation(exception, $"Removing unrecoverable {peer}.");
            await _connectionService.TrySchedulePeerReconnectionAsync(peer);
        }
        else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
        {
            Logger.LogDebug(exception, $"Queuing peer for reconnection {peer.RemoteEndpoint}.");
            _taskQueueManager.Enqueue(async () => await RecoverPeerAsync(peer), NetworkConstants.PeerReconnectionQueueName);
        }
    }

    private async Task RecoverPeerAsync(IPeer peer)
    {
        if (peer.IsReady) // peer recovered already
            return;
        var success = await peer.TryRecoverAsync();
        if (!success) await _connectionService.TrySchedulePeerReconnectionAsync(peer);
    }

    private async Task<bool> ProcessStreamRequest(StreamMessage reply)
    {
        if (reply.StreamType is not (StreamType.HandShakeReply or StreamType.DisconnectReply or StreamType.PongReply or StreamType.BlockBroadcastReply or StreamType.TransactionBroadcastReply or StreamType.AnnouncementBroadcastReply
            or StreamType.LibAnnouncementBroadcastReply or StreamType.ConfirmHandShakeReply or StreamType.HealthCheckReply or StreamType.RequestBlockReply or StreamType.RequestBlocksReply or StreamType.GetNodesReply)) return false;
        Logger.LogInformation("receive {RequestId} {streamType}", reply.RequestId, reply.StreamType);
        _streamTaskResourcePool.TrySetResult(reply.RequestId, reply);
        return true;
    }

    private async Task ProcessStreamRequest(StreamMessage reply, IAsyncStreamWriter<StreamMessage> responseStream)
    {
        switch (reply.StreamType)
        {
            case StreamType.HandShakeReply:
            case StreamType.DisconnectReply:
            case StreamType.PongReply:
            case StreamType.BlockBroadcastReply:
            case StreamType.TransactionBroadcastReply:
            case StreamType.AnnouncementBroadcastReply:
            case StreamType.LibAnnouncementBroadcastReply:
            case StreamType.ConfirmHandShakeReply:
            case StreamType.HealthCheckReply:
            case StreamType.RequestBlockReply:
            case StreamType.RequestBlocksReply:
            case StreamType.GetNodesReply:
                Logger.LogWarning("receive {RequestId}", reply.RequestId);
                _streamTaskResourcePool.TrySetResult(reply.RequestId, reply);
                break;
            // case StreamType.HandShake:       impossible！！！    i
            //     var handshakeReply = await _connectionService.DoHandshakeByStreamAsync(new DnsEndPoint(reply.Meta[GrpcConstants.StreamPeerHostKey], int.Parse(reply.Meta[GrpcConstants.StreamPeerPortKey])), responseStream,
            //         Handshake.Parser.ParseFrom(reply.Body));
            //     await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.HandShakeReply, RequestId = reply.RequestId, Body = handshakeReply.ToByteString() });
            //     return;
            case StreamType.GetNodes:
                var nodeList = await GetNodes(NodesRequest.Parser.ParseFrom(reply.Body), reply.Meta[GrpcConstants.PeerInfoMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.GetNodesReply, RequestId = reply.RequestId, Body = nodeList.ToByteString() });
                break;
            case StreamType.HealthCheck:
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.HealthCheckReply, RequestId = reply.RequestId, Body = new HealthCheckReply().ToByteString() });
                break;
            case StreamType.Ping:
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.PongReply, RequestId = reply.RequestId, Body = new PongReply().ToByteString() });
                break;
            case StreamType.RequestBlock:
                var block = await RequestBlock(BlockRequest.Parser.ParseFrom(reply.Body), reply.RequestId, reply.Meta[GrpcConstants.PeerInfoMetadataKey], reply.Meta[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.RequestBlockReply, RequestId = reply.RequestId, Body = block.ToByteString() });
                break;
            case StreamType.RequestBlocks:
                var blocks = await RequestBlocks(BlocksRequest.Parser.ParseFrom(reply.Body), reply.RequestId, reply.Meta[GrpcConstants.PeerInfoMetadataKey]);
                //todo fix me SetGrpcGzip(context);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.RequestBlocksReply, RequestId = reply.RequestId, Body = blocks.ToByteString() });
                break;
            case StreamType.Disconnect:
                await Disconnect(DisconnectReason.Parser.ParseFrom(reply.Body), reply.RequestId, reply.Meta?[GrpcConstants.PeerInfoMetadataKey], reply.Meta?[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.DisconnectReply, RequestId = reply.RequestId, Body = new VoidReply().ToByteString() });
                break;
            case StreamType.ConfirmHandShake:
                ConfirmHandshake(reply.RequestId, reply.Meta[GrpcConstants.PeerInfoMetadataKey], reply.Meta[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.ConfirmHandShakeReply, RequestId = reply.RequestId, Body = new VoidReply().ToByteString() });
                break;
            case StreamType.BlockBroadcast:
                await ProcessBlockAsync(BlockWithTransactions.Parser.ParseFrom(reply.Body), reply.Meta[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.BlockBroadcastReply, RequestId = reply.RequestId, Body = new VoidReply().ToByteString() });
                break;
            case StreamType.AnnouncementBroadcast:
                await ProcessAnnouncementAsync(BlockAnnouncement.Parser.ParseFrom(reply.Body), reply.Meta[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.AnnouncementBroadcastReply, RequestId = reply.RequestId, Body = new VoidReply().ToByteString() });
                break;
            case StreamType.TransactionBroadcast:
                await ProcessTransactionAsync(Transaction.Parser.ParseFrom(reply.Body), reply.Meta[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.TransactionBroadcastReply, RequestId = reply.RequestId, Body = new VoidReply().ToByteString() });
                break;
            case StreamType.LibAnnouncementBroadcast:
                await ProcessLibAnnouncementAsync(LibAnnouncement.Parser.ParseFrom(reply.Body), reply.Meta[GrpcConstants.PubkeyMetadataKey]);
                await responseStream.WriteAsync(new StreamMessage { StreamType = StreamType.LibAnnouncementBroadcastReply, RequestId = reply.RequestId, Body = new VoidReply().ToByteString() });
                break;
            case StreamType.Unknown:
            default:
                Logger.LogWarning("unsupported impl type={type}", reply.StreamType);
                break;
        }
    }

    private Task ProcessBlockAsync(BlockWithTransactions block, string peerPubkey)
    {
        var peer = TryGetPeerByPubkey(peerPubkey);

        if (peer.SyncState != SyncState.Finished) peer.SyncState = SyncState.Finished;

        if (!peer.TryAddKnownBlock(block.GetHash()))
            return Task.CompletedTask;

        _ = EventBus.PublishAsync(new BlockReceivedEvent(block, peerPubkey));
        return Task.CompletedTask;
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

        if (peer.SyncState != SyncState.Finished) peer.SyncState = SyncState.Finished;

        _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(announcement, peerPubkey));

        return Task.CompletedTask;
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

        _ = EventBus.PublishAsync(new TransactionsReceivedEvent { Transactions = new List<Transaction> { tx } });
    }

    private Task ProcessLibAnnouncementAsync(LibAnnouncement announcement, string peerPubkey)
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

    private async Task<HandshakeReply> ProcessHandShake(StreamMessage request, IAsyncStreamWriter<StreamMessage> responseStream, ServerCallContext context)
    {
        try
        {
            Logger.LogDebug($"Peer {context.Peer} has requested a handshake.");

            if (context.AuthContext?.Properties != null)
                foreach (var authProperty in context.AuthContext.Properties)
                    Logger.LogDebug($"Auth property: {authProperty.Name} -> {authProperty.Value}");

            if (!GrpcEndPointHelper.ParseDnsEndPoint(context.Peer, out var peerEndpoint))
                return new HandshakeReply { Error = HandshakeError.InvalidConnection };
            return await _connectionService.DoHandshakeByStreamAsync(peerEndpoint, responseStream, HandshakeRequest.Parser.ParseFrom(request.Body).Handshake);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, $"Handshake failed - {context.Peer}: ");
            throw;
        }
    }

    /// <summary>
    ///     This method returns a block. The parameter is a <see cref="BlockRequest" /> object, if the value
    ///     of <see cref="BlockRequest.Hash" /> is not null, the request is by ID, otherwise it will be
    ///     by height.
    /// </summary>
    private async Task<BlockReply> RequestBlock(BlockRequest request, string requestId, string peerInfo, string pubKey)
    {
        if (request == null || request.Hash == null || _syncStateService.SyncState != SyncState.Finished)
            return new BlockReply();
        Logger.LogDebug("Peer {peerInfo} requested block {hash}. requestId={requestId}", peerInfo, request.Hash, requestId);
        BlockWithTransactions block;
        try
        {
            block = await _blockchainService.GetBlockWithTransactionsByHashAsync(request.Hash);

            if (block == null)
            {
                Logger.LogDebug("Could not find block {hash} for {peerInfo}. requestId={requestId}", request.Hash, peerInfo, requestId);
            }
            else
            {
                var peer = _connectionService.GetPeerByPubkey(pubKey);
                peer.TryAddKnownBlock(block.GetHash());
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Request block error: block {hash} for {peerInfo}. requestId={requestId}", request.Hash, peerInfo, requestId);
            throw;
        }

        return new BlockReply { Block = block };
    }

    private async Task<BlockList> RequestBlocks(BlocksRequest request, string requestId, string peerInfo)
    {
        if (request == null ||
            request.PreviousBlockHash == null ||
            _syncStateService.SyncState != SyncState.Finished ||
            request.Count == 0 ||
            request.Count > GrpcConstants.MaxSendBlockCountLimit)
            return new BlockList();

        Logger.LogDebug(
            "Peer {peerInfo} requested {count} blocks from {preHash}. requestId={requestId}", peerInfo, request.Count, request.PreviousBlockHash, requestId);

        var blockList = new BlockList();

        try
        {
            var blocks =
                await _blockchainService.GetBlocksWithTransactionsAsync(request.PreviousBlockHash, request.Count);

            blockList.Blocks.AddRange(blocks);
            Logger.LogDebug(
                "Replied to {peerInfo} with {count}, request was {request}. requestId={requestId}", peerInfo, blockList.Blocks.Count, request, requestId);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Request blocks error - {peerInfo} - request={request}. requestId={requestId} ", peerInfo, request, requestId);
            throw;
        }

        return blockList;
    }

    private async Task SetGrpcGzip(ServerCallContext context)
    {
        if (NetworkOptions.CompressBlocksOnRequest)
        {
            var headers = new Metadata
                { new(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst) };
            await context.WriteResponseHeadersAsync(headers);
        }
    }

    private async Task<NodeList> GetNodes(NodesRequest request, string peerInfo)
    {
        if (request == null)
            return new NodeList();

        var nodesCount = Math.Min(request.MaxCount, GrpcConstants.DefaultDiscoveryMaxNodesToResponse);
        Logger.LogDebug("Peer {peerInfo} requested {nodesCount} nodes.", peerInfo, nodesCount);

        NodeList nodes;
        try
        {
            nodes = await _nodeManager.GetRandomNodesAsync(nodesCount);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Get nodes error: ");
            throw;
        }

        Logger.LogDebug("Sending {nodes.Nodes.Count} to {peerInfo}.", peerInfo);
        return nodes;
    }

    private void ConfirmHandshake(string requestId, string peerInfo, string pubKey)
    {
        try
        {
            Logger.LogDebug("Peer {peerInfo} has requested a handshake confirmation. requestId={requestId}", peerInfo, requestId);
            _connectionService.ConfirmHandshake(pubKey);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Confirm handshake error - {peerInfo}: requestId={requestId}", peerInfo, requestId);
            throw;
        }
    }

    private async Task Disconnect(DisconnectReason request, string requestId, string peerInfo, string pubKey)
    {
        Logger.LogDebug("Peer {peerInfo} has sent a disconnect request. reason={reason} requestId={requestId}", peerInfo, request, requestId);
        try
        {
            await _connectionService.RemovePeerAsync(pubKey);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "requestId={requestId}, Disconnect error: ", requestId);
            throw;
        }
    }

    /// <summary>
    ///     Try to get the peer based on pubkey.
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