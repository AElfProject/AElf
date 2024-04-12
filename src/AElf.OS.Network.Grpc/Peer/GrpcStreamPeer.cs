using System;
using System.Collections.Generic;
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

public delegate void StreamSendCallBack(NetworkException ex, StreamMessage streamMessage, int callTimes = 0);

public class GrpcStreamPeer : GrpcPeer
{
    private const int StreamWaitTime = 3000;
    private const int TimeOutRetryTimes = 3;

    private AsyncDuplexStreamingCall<StreamMessage, StreamMessage> _duplexStreamingCall;
    private CancellationTokenSource _streamListenTaskTokenSource;
    private IAsyncStreamWriter<StreamMessage> _clientStreamWriter;

    private readonly IStreamTaskResourcePool _streamTaskResourcePool;
    private readonly Dictionary<string, string> _peerMeta;
    private StreamSendCallBack _streamSendCallBack;

    protected readonly ActionBlock<StreamJob> _sendStreamJobs;
    protected bool IsClosed = false;
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
        var headers = new Metadata { new(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst) };
        _duplexStreamingCall = _client.RequestByStream(new CallOptions().WithHeaders(headers).WithDeadline(DateTime.MaxValue));
        _clientStreamWriter = _duplexStreamingCall.RequestStream;
        IsClosed = false;
        return _duplexStreamingCall;
    }

    public void StartServe(CancellationTokenSource listenTaskTokenSource)
    {
        _streamListenTaskTokenSource = listenTaskTokenSource;
    }

    public void SetStreamSendCallBack(StreamSendCallBack callBack)
    {
        _streamSendCallBack = callBack;
    }

    public async Task DisposeAsync()
    {
        _sendStreamJobs.Complete();
        await _duplexStreamingCall?.RequestStream?.CompleteAsync();
        _duplexStreamingCall?.Dispose();
        _streamListenTaskTokenSource?.Cancel();
        IsClosed = true;
    }

    public override async Task DisconnectAsync(bool gracefulDisconnect)
    {
        try
        {
            await RequestAsync(() => StreamRequestAsync(MessageType.Disconnect,
                    new DisconnectReason { Why = DisconnectReason.Types.Reason.Shutdown },
                    new Metadata { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } }, null, true),
                new GrpcRequest { ErrorMessage = "Could not send disconnect." });
            await DisposeAsync();
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
        var requestId = CommonHelper.GenerateRequestId();
        var grpcRequest = new GrpcRequest { ErrorMessage = $"handshake failed.requestId={requestId}" };
        var reply = await RequestAsync(() => StreamRequestAsync(MessageType.HandShake, request, metadata, requestId, true), grpcRequest);
        return reply != null ? HandshakeReply.Parser.ParseFrom(reply.Message) : new HandshakeReply() { Error = HandshakeError.InvalidConnection };
    }

    public override async Task ConfirmHandshakeAsync()
    {
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"Could not send confirm handshake.requestId={requestId}" };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, UpdateHandshakeTimeout.ToString() },
        };
        var reply = await RequestAsync(() => StreamRequestAsync(MessageType.ConfirmHandShake, new ConfirmHandshakeRequest(), data, requestId, true), request);
        if (reply == null) throw new Exception("confirm handshake failed");
    }

    protected override async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"broadcast block failed.requestId={requestId}" };
        await RequestAsync(() => StreamRequestAsync(MessageType.BlockBroadcast, blockWithTransactions, null, requestId), request);
    }

    protected override async Task SendAnnouncementAsync(BlockAnnouncement header)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"broadcast block announcement failed.requestId={requestId}" };
        await RequestAsync(() => StreamRequestAsync(MessageType.AnnouncementBroadcast, header, null, requestId), request);
    }

    protected override async Task SendTransactionAsync(Transaction transaction)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"broadcast transaction failed.requestId={requestId}" };
        await RequestAsync(() => StreamRequestAsync(MessageType.TransactionBroadcast, transaction, null, requestId), request);
    }

    public override async Task SendLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"broadcast lib announcement failed. requestId={requestId}" };
        await RequestAsync(() => StreamRequestAsync(MessageType.LibAnnouncementBroadcast, libAnnouncement, null, requestId), request);
    }


    public override async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash)
    {
        var blockRequest = new BlockRequest { Hash = hash };
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest
        {
            ErrorMessage = $"Block request for {hash} failed. requestId={requestId}",
            MetricName = nameof(MetricNames.GetBlock),
            MetricInfo = $"Block request for {hash}"
        };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, BlockRequestTimeout.ToString() },
        };
        var blockMessage = await RequestAsync(() => StreamRequestAsync(MessageType.RequestBlock, blockRequest, data, requestId), request);
        return blockMessage != null ? BlockReply.Parser.ParseFrom(blockMessage.Message).Block : new BlockWithTransactions();
    }

    public override async Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"Request nodes failed.requestId={requestId}" };
        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, GetNodesTimeout.ToString() },
        };
        var listMessage = await RequestAsync(() => StreamRequestAsync(MessageType.GetNodes, new NodesRequest { MaxCount = count }, data, requestId), request);
        return listMessage != null ? NodeList.Parser.ParseFrom(listMessage.Message) : null;
    }

    public override async Task CheckHealthAsync()
    {
        await base.CheckHealthAsync();
        var requestId = CommonHelper.GenerateRequestId();
        var request = new GrpcRequest { ErrorMessage = $"Check health failed.requestId={requestId}" };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, CheckHealthTimeout.ToString() },
        };
        await RequestAsync(() => StreamRequestAsync(MessageType.HealthCheck, new HealthCheckRequest(), data, requestId), request);
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
            if (!(job.StreamMessage.StreamType == StreamType.Reply && job.StreamMessage.MessageType == MessageType.RequestBlocks))
                _clientStreamWriter.WriteOptions = new WriteOptions(WriteFlags.NoCompress);
            await _clientStreamWriter.WriteAsync(job.StreamMessage);
        }
        catch (RpcException ex)
        {
            job.SendCallback?.Invoke(HandleRpcException(ex, $"Could not write to stream to {this}, queueCount={_sendStreamJobs.InputCount}"));
            return;
        }
        catch (Exception e)
        {
            var type = e switch
            {
                InvalidOperationException => NetworkExceptionType.Unrecoverable,
                TimeoutException => NetworkExceptionType.PeerUnstable,
                _ => NetworkExceptionType.HandlerException
            };
            job.SendCallback?.Invoke(
                new NetworkException($"{job.StreamMessage?.RequestId}{job.StreamMessage?.StreamType}-{job.StreamMessage?.MessageType} size={job.StreamMessage.ToByteArray().Length} queueCount={_sendStreamJobs.InputCount}", e, type));
            await Task.Delay(StreamRecoveryWaitTime);
            return;
        }

        job.SendCallback?.Invoke(null);
    }

    protected async Task<TResp> RequestAsync<TResp>(Func<Task<TResp>> func, GrpcRequest request)
    {
        var recordRequestTime = !string.IsNullOrEmpty(request.MetricName);
        var requestStartTime = TimestampHelper.GetUtcNow();
        var resp = await func();
        if (recordRequestTime)
            RecordMetric(request, requestStartTime, (TimestampHelper.GetUtcNow() - requestStartTime).Milliseconds());
        return resp;
    }

    protected async Task<StreamMessage> StreamRequestAsync(MessageType messageType, IMessage message, Metadata header = null, string requestId = null, bool graceful = false)
    {
        if (IsClosed) return null;
        for (var i = 0; i < TimeOutRetryTimes; i++)
        {
            requestId = requestId == null || i > 0 ? CommonHelper.GenerateRequestId() : requestId;
            var streamMessage = new StreamMessage { StreamType = StreamType.Request, MessageType = messageType, RequestId = requestId, Message = message.ToByteString() };
            AddAllHeaders(streamMessage, header);
            TaskCompletionSource<StreamMessage> promise = new TaskCompletionSource<StreamMessage>();
            await _streamTaskResourcePool.RegistryTaskPromiseAsync(requestId, messageType, promise);

            await _sendStreamJobs.SendAsync(new StreamJob
            {
                StreamMessage = streamMessage, SendCallback = ex =>
                {
                    if (ex != null && !graceful) _streamSendCallBack?.Invoke(ex, streamMessage, i);
                }
            });
            var result = await _streamTaskResourcePool.GetResultAsync(promise, requestId, GetTimeOutFromHeader(header));
            if (result.Item1)
            {
                if (!graceful) _streamSendCallBack?.Invoke(null, streamMessage, i);
                return result.Item2;
            }

            if (i >= TimeOutRetryTimes - 1)
            {
                if (!graceful) _streamSendCallBack?.Invoke(new NetworkException($"streaming call time out requestId {requestId}-{messageType}-{this}", null, NetworkExceptionType.PeerUnstable), streamMessage, i);
                return null;
            }

            if (graceful) return null;
            await Task.Delay(StreamRecoveryWaitTime);
        }

        return null;
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

    public override NetworkException HandleRpcException(RpcException exception, string errorMessage)
    {
        var message = $"Failed request to {this}: {errorMessage}";
        NetworkExceptionType type;

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
            switch (exception.StatusCode)
            {
                case StatusCode.Unavailable:
                case StatusCode.Cancelled:
                    message = $"Request was cancelled {this}: {errorMessage}";
                    type = NetworkExceptionType.Unrecoverable;
                    break;
                case StatusCode.Internal:
                    message = $"internal exception {this}: {errorMessage}";
                    type = NetworkExceptionType.PeerUnstable;
                    break;
                case StatusCode.DeadlineExceeded:
                    message = $"stream call timeout {this} : {errorMessage}";
                    type = NetworkExceptionType.PeerUnstable;
                    break;
                default:
                    message = $"Exception in handler  {this} : {errorMessage}";
                    type = NetworkExceptionType.HandlerException;
                    break;
            }
        }

        return new NetworkException(message, exception, type);
    }

    public override string ToString()
    {
        return $"{{ streamPeer listening-port: {RemoteEndpoint}, key: {Info.Pubkey.Substring(0, 45)}... }}";
    }
}