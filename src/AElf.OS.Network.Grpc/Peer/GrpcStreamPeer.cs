using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.Network.Grpc;

public class GrpcStreamPeer : GrpcPeer
{
    private const int StreamWaitTime = 500;
    private AsyncDuplexStreamingCall<StreamMessage, StreamMessage> _duplexStreamingCall;
    private CancellationTokenSource _streamListenTaskTokenSource;
    private IAsyncStreamWriter<StreamMessage> _clientStreamWriter;

    private readonly IStreamTaskResourcePool _streamTaskResourcePool;
    private readonly Dictionary<string, string> _peerMeta;

    protected readonly ActionBlock<StreamJob> _sendStreamJobs;
    public ILogger<GrpcStreamPeer> Logger { get; set; }

    public GrpcStreamPeer(GrpcClient client, DnsEndPoint remoteEndpoint, PeerConnectionInfo peerConnectionInfo,
        AsyncDuplexStreamingCall<StreamMessage, StreamMessage> duplexStreamingCall,
        IAsyncStreamWriter<StreamMessage> clientStreamWriter,
        IStreamTaskResourcePool streamTaskResourcePool, Dictionary<string, string> peerMeta) : base(client,
        remoteEndpoint, peerConnectionInfo)
    {
        _duplexStreamingCall = duplexStreamingCall;
        _clientStreamWriter = duplexStreamingCall?.RequestStream ?? clientStreamWriter;
        _streamTaskResourcePool = streamTaskResourcePool;
        _peerMeta = peerMeta;
        _sendStreamJobs = new ActionBlock<StreamJob>(WriteStreamJobAsync, new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = NetworkConstants.DefaultMaxBufferedStreamCount
        });
        Logger = NullLogger<GrpcStreamPeer>.Instance;
    }

    public AsyncDuplexStreamingCall<StreamMessage, StreamMessage> BuildCall()
    {
        if (_client == null) return null;
        _duplexStreamingCall = _client.RequestByStream(new CallOptions().WithDeadline(DateTime.MaxValue));
        _clientStreamWriter = _duplexStreamingCall.RequestStream;
        return _duplexStreamingCall;
    }

    public void StartServe(CancellationTokenSource listenTaskTokenSource)
    {
        _streamListenTaskTokenSource = listenTaskTokenSource;
    }

    public override async Task DisconnectAsync(bool gracefulDisconnect)
    {
        try
        {
            await RequestAsync(() => StreamRequestAsync(MessageType.Disconnect,
                    new DisconnectReason { Why = DisconnectReason.Types.Reason.Shutdown },
                    new Metadata { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } }),
                new GrpcRequest { ErrorMessage = "Could not send disconnect." });
            _sendStreamJobs.Complete();
            await _duplexStreamingCall?.RequestStream?.CompleteAsync();
            _duplexStreamingCall?.Dispose();
            _streamListenTaskTokenSource?.Cancel();
        }
        catch (Exception)
        {
            // swallow the exception, we don't care because we're disconnecting.
        }

        await base.DisconnectAsync(gracefulDisconnect);
    }

    public async Task<HandshakeReply> HandShakeAsync(HandshakeRequest request)
    {
        var metadata = new Metadata
        {
            { GrpcConstants.RetryCountMetadataKey, "0" },
        };
        var grpcRequest = new GrpcRequest { ErrorMessage = "handshake failed." };
        var reply = await RequestAsync(() => StreamRequestAsync(MessageType.HandShake, request, metadata), grpcRequest);
        return HandshakeReply.Parser.ParseFrom(reply.Message);
    }

    public override async Task ConfirmHandshakeAsync()
    {
        var request = new GrpcRequest { ErrorMessage = "Could not send confirm handshake." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, UpdateHandshakeTimeout.ToString() },
        };
        await RequestAsync(() => StreamRequestAsync(MessageType.ConfirmHandShake, new ConfirmHandshakeRequest(), data), request);
    }

    protected override async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        var request = new GrpcRequest { ErrorMessage = "broadcast block failed." };
        await RequestAsync(() => StreamRequestAsync(MessageType.BlockBroadcast, blockWithTransactions), request);
    }

    protected override async Task SendAnnouncementAsync(BlockAnnouncement header)
    {
        var request = new GrpcRequest { ErrorMessage = "broadcast block announcement failed." };
        await RequestAsync(() => StreamRequestAsync(MessageType.AnnouncementBroadcast, header), request);
    }

    protected override async Task SendTransactionAsync(Transaction transaction)
    {
        var request = new GrpcRequest { ErrorMessage = "broadcast transaction failed." };
        await RequestAsync(() => StreamRequestAsync(MessageType.TransactionBroadcast, transaction), request);
    }

    public override async Task SendLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        var request = new GrpcRequest { ErrorMessage = "broadcast lib announcement failed." };
        await RequestAsync(() => StreamRequestAsync(MessageType.LibAnnouncementBroadcast, libAnnouncement), request);
    }

    public override async Task<List<BlockWithTransactions>> GetBlocksAsync(Hash firstHash, int count)
    {
        var blockRequest = new BlocksRequest { PreviousBlockHash = firstHash, Count = count };
        var blockInfo = $"{{ first: {firstHash}, count: {count} }}";

        var request = new GrpcRequest
        {
            ErrorMessage = $"Get blocks for {blockInfo} failed.",
            MetricName = nameof(MetricNames.GetBlocks),
            MetricInfo = $"Get blocks for {blockInfo}"
        };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, BlocksRequestTimeout.ToString() },
        };
        var listMessage = await RequestAsync(() => StreamRequestAsync(MessageType.RequestBlocks, blockRequest, data), request);
        return BlockList.Parser.ParseFrom(listMessage.Message).Blocks.ToList();
    }

    public override async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash)
    {
        var blockRequest = new BlockRequest { Hash = hash };

        var request = new GrpcRequest
        {
            ErrorMessage = $"Block request for {hash} failed.",
            MetricName = nameof(MetricNames.GetBlock),
            MetricInfo = $"Block request for {hash}"
        };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, BlockRequestTimeout.ToString() },
        };
        var blockMessage = await RequestAsync(() => StreamRequestAsync(MessageType.RequestBlock, blockRequest, data), request);
        return BlockReply.Parser.ParseFrom(blockMessage.Message).Block;
    }

    public override async Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest)
    {
        var request = new GrpcRequest { ErrorMessage = "Request nodes failed." };
        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, GetNodesTimeout.ToString() },
        };
        var listMessage = await RequestAsync(() => StreamRequestAsync(MessageType.GetNodes, new NodesRequest { MaxCount = count }, data), request);
        return NodeList.Parser.ParseFrom(listMessage.Message);
    }

    public override async Task CheckHealthAsync()
    {
        await base.CheckHealthAsync();
        var request = new GrpcRequest { ErrorMessage = "Check health failed." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, CheckHealthTimeout.ToString() },
        };
        await RequestAsync(() => StreamRequestAsync(MessageType.HealthCheck, new HealthCheckRequest(), data), request);
    }

    public async Task WriteAsync(StreamMessage message, Action<NetworkException> sendCallback)
    {
        await _sendStreamJobs.SendAsync(new StreamJob { StreamMessage = message, SendCallback = sendCallback });
    }

    private async Task WriteStreamJobAsync(StreamJob job)
    {
        //avoid write stream concurrency
        try
        {
            if (job.StreamMessage == null) return;
            Logger.LogDebug("write request={requestId} {streamType}-{messageType}", job.StreamMessage.RequestId, job.StreamMessage.StreamType, job.StreamMessage.MessageType);
            await _clientStreamWriter.WriteAsync(job.StreamMessage);
        }
        catch (RpcException ex)
        {
            job.SendCallback?.Invoke(HandleRpcException(ex, $"Could not write to stream to {this}: "));
            await Task.Delay(StreamRecoveryWaitTime);
            return;
        }

        job.SendCallback?.Invoke(null);
    }

    protected async Task<TResp> RequestAsync<TResp>(Func<Task<TResp>> func, GrpcRequest request)
    {
        var recordRequestTime = !string.IsNullOrEmpty(request.MetricName);
        var requestStartTime = TimestampHelper.GetUtcNow();
        try
        {
            var resp = await func();
            if (recordRequestTime)
                RecordMetric(request, requestStartTime, (TimestampHelper.GetUtcNow() - requestStartTime).Milliseconds());
            return resp;
        }
        catch (RpcException e)
        {
            throw HandleRpcException(e, request.ErrorMessage);
        }
        catch (Exception)
        {
            // if (e is TimeoutException or InvalidOperationException)
            throw HandleRpcException(new RpcException(new Status(StatusCode.Unknown, request.ErrorMessage), request.ErrorMessage), request.ErrorMessage);
        }
    }

    protected async Task<StreamMessage> StreamRequestAsync(MessageType messageType, IMessage message, Metadata header = null)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var streamMessage = new StreamMessage { StreamType = StreamType.Request, MessageType = messageType, RequestId = requestId, Message = message.ToByteString() };
        AddAllHeaders(streamMessage, header);
        var promise = new TaskCompletionSource<StreamMessage>();
        await _streamTaskResourcePool.RegistryTaskPromiseAsync(requestId, messageType, promise);
        await _sendStreamJobs.SendAsync(new StreamJob
        {
            StreamMessage = streamMessage, SendCallback = ex =>
            {
                if (ex != null) throw ex;
            }
        });
        return await _streamTaskResourcePool.GetResultAsync(promise, requestId, GetTimeOutFromHeader(header));
    }

    private void AddAllHeaders(StreamMessage streamMessage, Metadata metadata = null)
    {
        foreach (var kv in _peerMeta)
        {
            streamMessage.Meta[kv.Key] = kv.Value;
        }

        streamMessage.Meta[GrpcConstants.SessionIdMetadataKey] = OutboundSessionId.ToHex();
        if (metadata == null) return;
        foreach (var e in metadata)
        {
            if (e.IsBinary)
            {
                streamMessage.Meta[e.Key] = e.ValueBytes.ToHex();
                continue;
            }

            streamMessage.Meta[e.Key] = e.Value;
        }
    }

    private int GetTimeOutFromHeader(Metadata header)
    {
        if (header == null) return StreamWaitTime;
        var t = header.Get(GrpcConstants.TimeoutMetadataKey)?.Value;
        return t == null ? StreamWaitTime : int.Parse(t);
    }

    protected override NetworkException HandleRpcException(RpcException exception, string errorMessage)
    {
        var message = $"Failed request to {this}: {errorMessage}";
        var type = NetworkExceptionType.Rpc;

        if (_channel.State != ChannelState.Ready)
        {
            // if channel has been shutdown (unrecoverable state) remove it.
            if (_channel.State == ChannelState.Shutdown)
            {
                message = $"Peer is shutdown - {this}: {errorMessage}";
                type = NetworkExceptionType.Unrecoverable;
            }
            else if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
            {
                // from this we try to recover
                message = $"Peer is unstable - {this}: {errorMessage}";
                type = NetworkExceptionType.PeerUnstable;
            }
            else
            {
                // if idle just after an exception, disconnect.
                message = $"Peer idle, channel state {_channel.State} - {this}: {errorMessage}";
                type = NetworkExceptionType.Unrecoverable;
            }
        }
        else
        {
            message = $"Peer is ready, but stream is unstable - {this}: {errorMessage}";
            type = NetworkExceptionType.PeerUnstable;
        }

        return new NetworkException(message, exception, type);
    }

    public override async Task<bool> TryRecoverAsync()
    {
        return IsReady || await base.TryRecoverAsync();
    }
}