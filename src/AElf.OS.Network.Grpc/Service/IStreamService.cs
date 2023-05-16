using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Grpc;

public interface IStreamService
{
    Task ProcessStreamReplyAsync(ByteString reply, string clientPubKey);
    Task ProcessStreamRequestAsync(StreamMessage request, ServerCallContext context);
}

public class StreamService : IStreamService, ISingletonDependency
{
    public ILogger<StreamService> Logger { get; set; }
    private readonly IConnectionService _connectionService;
    private readonly IStreamTaskResourcePool _streamTaskResourcePool;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly Dictionary<MessageType, IStreamMethod> _streamMethods;


    public StreamService(IConnectionService connectionService, IStreamTaskResourcePool streamTaskResourcePool, ITaskQueueManager taskQueueManager,
        IEnumerable<IStreamMethod> streamMethods)
    {
        Logger = NullLogger<StreamService>.Instance;
        _connectionService = connectionService;
        _streamTaskResourcePool = streamTaskResourcePool;
        _taskQueueManager = taskQueueManager;
        _streamMethods = streamMethods.ToDictionary(x => x.Method, y => y);
    }

    public async Task ProcessStreamRequestAsync(StreamMessage request, ServerCallContext context)
    {
        var peer = _connectionService.GetPeerByPubkey(context.GetPublicKey());
        var streamPeer = peer as GrpcStreamPeer;
        Logger.LogInformation("receive {requestId} {streamType} {meta}", request.RequestId, request.StreamType, request.Meta);

        await DoProcessAsync(new StreamMessageMetaStreamContext(request.Meta), request, streamPeer);
    }

    public async Task ProcessStreamReplyAsync(ByteString reply, string clientPubKey)
    {
        var message = StreamMessage.Parser.ParseFrom(reply);
        Logger.LogInformation("receive {requestId} {streamType} {meta}", message.RequestId, message.StreamType, message.Meta);

        var peer = _connectionService.GetPeerByPubkey(clientPubKey);
        var streamPeer = peer as GrpcStreamPeer;
        try
        {
            await DoProcessAsync(new StreamMessageMetaStreamContext(message.Meta), message, streamPeer);
            Logger.LogInformation("handle stream call success, clientPubKey={clientPubKey} request={requestId} {streamType}-{messageType}", clientPubKey, message.RequestId, message.StreamType, message.MessageType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "handle stream call failed, clientPubKey={clientPubKey} request={requestId} {streamType}-{messageType}", clientPubKey, message.RequestId, message.StreamType, message.MessageType);
        }
    }

    private async Task DoProcessAsync(IStreamContext streamContext, StreamMessage request, GrpcStreamPeer responsePeer)
    {
        Logger.LogInformation("receive {requestId} {streamType}-{messageType}", request.RequestId, request.StreamType, request.MessageType);
        if (!ValidContext(request, streamContext, responsePeer)) return;
        switch (request.StreamType)
        {
            case StreamType.Reply:
                _streamTaskResourcePool.TrySetResult(request.RequestId, request);
                return;
            case StreamType.Request:
                await ProcessRequestAsync(request, responsePeer, streamContext);
                return;
            case StreamType.Unknown:
            default:
                Logger.LogWarning("unhandled stream request: {requestId} {streamType}-{messageType}", request.RequestId, request.StreamType, request.MessageType);
                return;
        }
    }


    private async Task ProcessRequestAsync(StreamMessage request, GrpcStreamPeer responsePeer, IStreamContext streamContext)
    {
        try
        {
            if (!_streamMethods.TryGetValue(request.MessageType, out var method))
                Logger.LogWarning("unhandled stream request: {requestId} {streamType}-{messageType}", request.RequestId, request.StreamType, request.MessageType);
            var reply = method == null ? new VoidReply() : await method.InvokeAsync(request, streamContext);
            var message = new StreamMessage
            {
                StreamType = StreamType.Reply, MessageType = request.MessageType,
                RequestId = request.RequestId, Message = reply == null ? new VoidReply().ToByteString() : reply.ToByteString()
            };
            if (IsNeedAuth(request)) message.Meta.Add(GrpcConstants.SessionIdMetadataKey, responsePeer.Info.SessionId.ToHex());
            await responsePeer.WriteAsync(message, async ex =>
            {
                if (ex != null) await HandleNetworkExceptionAsync(responsePeer, ex);
            });
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "request failed {requestId} {streamType}-{messageType}", request.RequestId, request.StreamType, request.MessageType);
            throw;
        }
    }


    private async Task HandleNetworkExceptionAsync(IPeer peer, NetworkException exception)
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
        if (success && peer is GrpcStreamPeer streamPeer)
        {
            await _connectionService.BuildStreamForPeerAsync(streamPeer);
        }

        if (!success) await _connectionService.TrySchedulePeerReconnectionAsync(peer);
    }

    private bool ValidContext(StreamMessage message, IStreamContext context, GrpcStreamPeer peer)
    {
        if (!IsNeedAuth(message)) return true;
        if (peer == null)
        {
            Logger.LogWarning("Could not find peer {pubKey}", context.GetPubKey());
            return false;
        }

        // check that the peers session is equal to one announced in the headers
        var sessionId = context.GetSessionId();

        if (peer.InboundSessionId.ToHex().Equals(sessionId))
        {
            context.SetPeerInfo(peer.ToString());
            return true;
        }

        if (peer.InboundSessionId == null)
        {
            Logger.LogWarning("Wrong inbound session id {peer}, {streamType}-{messageType}", context.GetPeerInfo(), message.StreamType, message.MessageType);
            return false;
        }

        if (sessionId == null)
        {
            Logger.LogWarning("Wrong inbound session id {peer}, {requestId}", peer, message.RequestId);
            return false;
        }

        Logger.LogWarning("Unequal session id, ({inboundSessionId} {infoSession} vs {sessionId}) {streamType}-{messageType} {pubkey}  {peer}", peer.InboundSessionId.ToHex(), peer.Info.SessionId.ToHex(),
            sessionId, message.StreamType, message.MessageType, peer.Info.Pubkey, peer);
        return false;
    }

    private bool IsNeedAuth(StreamMessage streamMessage)
    {
        if (streamMessage.StreamType == StreamType.Reply) return false;
        return streamMessage.MessageType != MessageType.Ping &&
               streamMessage.MessageType != MessageType.HandShake &&
               streamMessage.MessageType != MessageType.HealthCheck;
    }
}