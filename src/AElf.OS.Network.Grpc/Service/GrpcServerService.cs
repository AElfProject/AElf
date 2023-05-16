using System;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Grpc.Helpers;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc;

/// <summary>
///     Implementation of the grpc generated service. It contains the rpc methods
///     exposed to peers.
/// </summary>
public class GrpcServerService : PeerService.PeerServiceBase
{
    private readonly IConnectionService _connectionService;
    private readonly IStreamService _streamService;
    private readonly IGrpcRequestProcessor _grpcRequestProcessor;

    public GrpcServerService(IConnectionService connectionService, IStreamService streamService, IGrpcRequestProcessor grpcRequestProcessor)
    {
        _connectionService = connectionService;
        _streamService = streamService;
        _grpcRequestProcessor = grpcRequestProcessor;

        EventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<GrpcServerService>.Instance;
    }

    private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
    public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
    public ILocalEventBus EventBus { get; set; }
    public ILogger<GrpcServerService> Logger { get; set; }

    public override async Task<HandshakeReply> DoHandshake(HandshakeRequest request, ServerCallContext context)
    {
        try
        {
            Logger.LogDebug($"Peer {context.Peer} has requested a handshake.");
            var authReply = AuthHandshake(request, context);
            return authReply.Item1 ?? await _connectionService.DoHandshakeAsync(authReply.Item2, request.Handshake);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, $"Handshake failed - {context.Peer}: ");
            throw;
        }
    }

    private Tuple<HandshakeReply, DnsEndPoint> AuthHandshake(HandshakeRequest request, ServerCallContext context)
    {
        if (context.AuthContext?.Properties != null)
            foreach (var authProperty in context.AuthContext.Properties)
                Logger.LogDebug($"Auth property: {authProperty.Name} -> {authProperty.Value}");

        return !GrpcEndPointHelper.ParseDnsEndPoint(context.Peer, out var peerEndpoint)
            ? new Tuple<HandshakeReply, DnsEndPoint>(new HandshakeReply { Error = HandshakeError.InvalidConnection }, peerEndpoint)
            : new Tuple<HandshakeReply, DnsEndPoint>(null, peerEndpoint);
    }

    public override async Task RequestByStream(IAsyncStreamReader<StreamMessage> requestStream, IServerStreamWriter<StreamMessage> responseStream, ServerCallContext context)
    {
        Logger.LogDebug("RequestByStream started with {peer}.", context.Peer);
        try
        {
            await requestStream.ForEachAsync(async req =>
            {
                var start = DateTimeOffset.UtcNow;
                Logger.LogDebug("receive request={requestId} {streamType}-{messageType}", req.RequestId, req.StreamType, req.MessageType);
                if (req.MessageType == MessageType.HandShake)
                {
                    var realRequest = HandshakeRequest.Parser.ParseFrom(req.Message);
                    var authReply = AuthHandshake(realRequest, context);
                    var handshakeReply = authReply.Item1 ?? await _connectionService.DoHandshakeByStreamAsync(authReply.Item2, responseStream, realRequest.Handshake);
                    Logger.LogDebug("request invoke success {reply}", handshakeReply.Handshake);
                    await responseStream.WriteAsync(new StreamMessage
                    {
                        // other stream message will come after handshake, so this write will not called concurrently with others
                        StreamType = StreamType.Reply, MessageType = req.MessageType,
                        RequestId = req.RequestId, Message = handshakeReply.ToByteString()
                    });
                }
                else
                {
                    await _streamService.ProcessStreamRequestAsync(req, context);
                }

                Logger.LogDebug("finish request={requestId} {streamType}-{messageType}, time cost={delta}", req.RequestId, req.StreamType, req.MessageType, DateTimeOffset.UtcNow.Subtract(start).TotalMilliseconds);
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "RequestByStream error - {peer}: ", context.Peer);
            throw;
        }

        Logger.LogDebug("RequestByStream finished with {peer}.", context.Peer);
    }

    public override Task<VoidReply> ConfirmHandshake(ConfirmHandshakeRequest request,
        ServerCallContext context)
    {
        return _grpcRequestProcessor.ConfirmHandshakeAsync(context.GetPeerInfo(), context.GetPublicKey());
    }

    public override async Task<VoidReply> BlockBroadcastStream(
        IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
    {
        try
        {
            Logger.LogDebug($"Block stream started with {context.GetPeerInfo()} - {context.Peer}.");
            var peerPubkey = context.GetPublicKey();
            await requestStream.ForEachAsync(async block => await _grpcRequestProcessor.ProcessBlockAsync(block, peerPubkey));
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"Block stream error - {context.GetPeerInfo()}: ");
            throw;
        }

        Logger.LogDebug($"Block stream finished with {context.GetPeerInfo()} - {context.Peer}.");

        return new VoidReply();
    }


    public override async Task<VoidReply> AnnouncementBroadcastStream(
        IAsyncStreamReader<BlockAnnouncement> requestStream, ServerCallContext context)
    {
        Logger.LogDebug($"Announcement stream started with {context.GetPeerInfo()} - {context.Peer}.");

        try
        {
            var peerPubkey = context.GetPublicKey();
            await requestStream.ForEachAsync(async r => await _grpcRequestProcessor.ProcessAnnouncementAsync(r, peerPubkey));
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"Announcement stream error: {context.GetPeerInfo()}");
            throw;
        }

        Logger.LogDebug($"Announcement stream finished with {context.GetPeerInfo()} - {context.Peer}.");

        return new VoidReply();
    }

    public override async Task<VoidReply> TransactionBroadcastStream(IAsyncStreamReader<Transaction> requestStream,
        ServerCallContext context)
    {
        Logger.LogDebug($"Transaction stream started with {context.GetPeerInfo()} - {context.Peer}.");

        try
        {
            var peerPubkey = context.GetPublicKey();
            await requestStream.ForEachAsync(async tx => await _grpcRequestProcessor.ProcessTransactionAsync(tx, peerPubkey));
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"Transaction stream error - {context.GetPeerInfo()}: ");
            throw;
        }

        Logger.LogDebug($"Transaction stream finished with {context.GetPeerInfo()} - {context.Peer}.");

        return new VoidReply();
    }

    public override async Task<VoidReply> LibAnnouncementBroadcastStream(
        IAsyncStreamReader<LibAnnouncement> requestStream, ServerCallContext context)
    {
        Logger.LogDebug($"Lib announcement stream started with {context.GetPeerInfo()} - {context.Peer}.");

        try
        {
            var peerPubkey = context.GetPublicKey();
            await requestStream.ForEachAsync(async r => await _grpcRequestProcessor.ProcessLibAnnouncementAsync(r, peerPubkey));
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"Lib announcement stream error: {context.GetPeerInfo()}");
            throw;
        }

        Logger.LogDebug($"Lib announcement stream finished with {context.GetPeerInfo()} - {context.Peer}.");

        return new VoidReply();
    }


    /// <summary>
    ///     This method returns a block. The parameter is a <see cref="BlockRequest" /> object, if the value
    ///     of <see cref="BlockRequest.Hash" /> is not null, the request is by ID, otherwise it will be
    ///     by height.
    /// </summary>
    public override async Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
    {
        return await _grpcRequestProcessor.GetBlockAsync(request, context.GetPeerInfo(), context.GetPublicKey());
    }

    public override async Task<BlockList> RequestBlocks(BlocksRequest request, ServerCallContext context)
    {
        var blocks = await _grpcRequestProcessor.GetBlocksAsync(request, context.GetPeerInfo());
        if (!NetworkOptions.CompressBlocksOnRequest) return blocks;
        var headers = new Metadata { new(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst) };
        await context.WriteResponseHeadersAsync(headers);
        return blocks;
    }

    public override async Task<NodeList> GetNodes(NodesRequest request, ServerCallContext context)
    {
        return await _grpcRequestProcessor.GetNodesAsync(request, context.GetPeerInfo());
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
    ///     Clients should call this method to disconnect explicitly.
    /// </summary>
    public override async Task<VoidReply> Disconnect(DisconnectReason request, ServerCallContext context)
    {
        return await _grpcRequestProcessor.DisconnectAsync(request, context.GetPeerInfo(), context.GetPublicKey());
    }
}