using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Metrics;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public class OutboundPeerHolder : IPeerHolder
{
    private const int MaxMetricsPerMethod = 100;

    private readonly PeerService.PeerServiceClient _client;
    private readonly Channel _channel;

    public bool IsReady => (_channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready) && IsConnected;
    public bool IsConnected { get; set; }
    public string ConnectionStatus => _channel.State.ToString();

    public bool ReConnectionAble => true;
    public PeerConnectionInfo Info { get; }

    public byte[] OutboundSessionId => Info.SessionId;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>> _recentRequestsRoundtripTimes;
    private AsyncClientStreamingCall<BlockAnnouncement, VoidReply> _announcementStreamCall;
    private AsyncClientStreamingCall<BlockWithTransactions, VoidReply> _blockStreamCall;
    private AsyncClientStreamingCall<LibAnnouncement, VoidReply> _libAnnouncementStreamCall;
    private AsyncClientStreamingCall<Transaction, VoidReply> _transactionStreamCall;
    private AsyncDuplexStreamingCall<StreamMessage, StreamMessage> _duplexStreamingCall;
    public IReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>> RecentRequestsRoundtripTimes { get; }

    private Task _serveTask;
    private CancellationTokenSource _streamListenTaskTokenSource;

    public OutboundPeerHolder(GrpcClient client, PeerConnectionInfo info)
    {
        _client = client.Client;
        _channel = client.Channel;
        Info = info;

        _recentRequestsRoundtripTimes = new ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>>();
        RecentRequestsRoundtripTimes =
            new ReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>>(_recentRequestsRoundtripTimes);

        _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlock), new ConcurrentQueue<RequestMetric>());
        _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlocks), new ConcurrentQueue<RequestMetric>());
    }

    public Task<NodeList> GetNodesAsync(NodesRequest nodesRequest, Metadata header, GrpcRequest request)
    {
        return RequestAsync(() => _client.GetNodesAsync(nodesRequest, header), request);
    }

    private async Task<TResp> RequestAsync<TResp>(Func<AsyncUnaryCall<TResp>> func, GrpcRequest requestParams)
    {
        var metricsName = requestParams.MetricName;
        var timeRequest = !string.IsNullOrEmpty(metricsName);
        var requestStartTime = TimestampHelper.GetUtcNow();

        Stopwatch requestTimer = null;

        if (timeRequest)
            requestTimer = Stopwatch.StartNew();

        try
        {
            var response = await func();
            if (timeRequest)
            {
                requestTimer.Stop();
                RecordMetric(requestParams, requestStartTime, requestTimer.ElapsedMilliseconds);
            }

            return response;
        }
        catch (ObjectDisposedException ex)
        {
            throw new NetworkException("Peer is closed", ex, NetworkExceptionType.Unrecoverable);
        }
        catch (AggregateException ex)
        {
            if (!(ex.InnerException is RpcException rpcException))
                throw new NetworkException($"Unknown exception. {this}: {requestParams.ErrorMessage}",
                    NetworkExceptionType.Unrecoverable);

            throw HandleRpcException(rpcException, requestParams.ErrorMessage);
        }
    }

    public NetworkException HandleRpcException(RpcException exception, string errorMessage)
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
            if (exception.StatusCode ==
                // there was an exception, not related to connectivity.
                StatusCode.Cancelled)
            {
                message = $"Request was cancelled {this}: {errorMessage}";
                type = NetworkExceptionType.Unrecoverable;
            }
            else if (exception.StatusCode == StatusCode.Unknown)
            {
                message = $"Exception in handler {this}: {errorMessage}";
                type = NetworkExceptionType.HandlerException;
            }
        }

        return new NetworkException(message, exception, type);
    }

    private void RecordMetric(GrpcRequest grpcRequest, Timestamp requestStartTime, long elapsedMilliseconds)
    {
        var metrics = _recentRequestsRoundtripTimes[grpcRequest.MetricName];

        while (metrics.Count >= MaxMetricsPerMethod)
            metrics.TryDequeue(out _);

        metrics.Enqueue(new RequestMetric
        {
            Info = grpcRequest.MetricInfo,
            RequestTime = requestStartTime,
            MethodName = grpcRequest.MetricName,
            RoundTripTime = elapsedMilliseconds
        });
    }

    public async Task Ping()
    {
        var stream = GetResponseStream();
        if (stream == null) return;
        await stream.WriteAsync(new StreamMessage { RequestId = CommonHelper.GenerateRequestId(), StreamType = StreamType.Ping, Body = new PingRequest().ToByteString() });
    }

    public Dictionary<string, List<RequestMetric>> GetRequestMetrics()
    {
        var metrics = new Dictionary<string, List<RequestMetric>>();
        foreach (var roundtripTime in _recentRequestsRoundtripTimes.ToArray())
        {
            var metricsToAdd = new List<RequestMetric>();

            metrics.Add(roundtripTime.Key, metricsToAdd);
            foreach (var requestMetric in roundtripTime.Value) metricsToAdd.Add(requestMetric);
        }

        return metrics;
    }

    public async Task CheckHealthAsync(Metadata header, GrpcRequest request)
    {
        await RequestAsync(() => _client.CheckHealthAsync(new HealthCheckRequest(), header), request);
    }


    public async Task<BlockWithTransactions> RequestBlockAsync(BlockRequest blockRequest, Metadata header, GrpcRequest request)
    {
        var blockReply = await RequestAsync(() => _client.RequestBlockAsync(blockRequest, header), request);
        return blockReply?.Block;
    }

    public async Task<BlockList> RequestBlocksAsync(BlocksRequest blocksRequest, Metadata header, GrpcRequest request)
    {
        return await RequestAsync(() => _client.RequestBlocksAsync(blocksRequest, header), request);
    }

    public async Task DisconnectAsync(bool gracefulDisconnect)
    {
        IsConnected = false;

        _announcementStreamCall?.Dispose();
        _transactionStreamCall?.Dispose();
        _blockStreamCall?.Dispose();
        _duplexStreamingCall?.RequestStream.CompleteAsync();
        _duplexStreamingCall?.Dispose();
        _streamListenTaskTokenSource?.Cancel();

        // send disconnect message if the peer is still connected and the connection
        // is stable.
        if (gracefulDisconnect && (_channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready))
        {
            var request = new GrpcRequest { ErrorMessage = "Could not send disconnect." };

            try
            {
                var metadata = new Metadata { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } };

                await RequestAsync(
                    () => _client.DisconnectAsync(new DisconnectReason
                        { Why = DisconnectReason.Types.Reason.Shutdown }, metadata), request);
            }
            catch (NetworkException)
            {
                // swallow the exception, we don't care because we're disconnecting.
            }
        }

        try
        {
            await _channel.ShutdownAsync();
        }
        catch (InvalidOperationException)
        {
            // if channel already shutdown
        }
    }

    public async Task ConfirmHandshakeAsync(ConfirmHandshakeRequest confirmHandshakeRequest, Metadata header, GrpcRequest request)
    {
        await RequestAsync(() => _client.ConfirmHandshakeAsync(new ConfirmHandshakeRequest(), header), request);
    }

    public async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        _blockStreamCall ??= _client.BlockBroadcastStream(new Metadata
            { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } });
        try
        {
            await _blockStreamCall.RequestStream.WriteAsync(blockWithTransactions);
        }
        catch (RpcException)
        {
            _blockStreamCall.Dispose();
            _blockStreamCall = null;

            throw;
        }
    }

    public async Task BroadcastAnnouncementBlockAsync(BlockAnnouncement header)
    {
        _announcementStreamCall ??= _client.AnnouncementBroadcastStream(new Metadata
            { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } });
        try
        {
            await _announcementStreamCall.RequestStream.WriteAsync(header);
        }
        catch (RpcException)
        {
            _announcementStreamCall.Dispose();
            _announcementStreamCall = null;

            throw;
        }
    }

    public async Task BroadcastTransactionAsync(Transaction transaction)
    {
        _transactionStreamCall ??= _client.TransactionBroadcastStream(new Metadata
            { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } });

        try
        {
            await _transactionStreamCall.RequestStream.WriteAsync(transaction);
        }
        catch (RpcException)
        {
            _transactionStreamCall.Dispose();
            _transactionStreamCall = null;

            throw;
        }
    }

    public async Task BroadcastLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        _libAnnouncementStreamCall ??= _client.LibAnnouncementBroadcastStream(new Metadata
            { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } });
        try
        {
            await _libAnnouncementStreamCall.RequestStream.WriteAsync(libAnnouncement);
        }
        catch (RpcException)
        {
            _libAnnouncementStreamCall.Dispose();
            _libAnnouncementStreamCall = null;

            throw;
        }
    }

    public async Task<bool> TryRecoverAsync()
    {
        if (_channel.State == ChannelState.Shutdown)
            return false;

        await _channel.TryWaitForStateChangedAsync(_channel.State,
            DateTime.UtcNow.AddSeconds(NetworkConstants.DefaultPeerRecoveryTimeout));

        // Either we connected again or the state change wait timed out.
        if (_channel.State != ChannelState.TransientFailure && _channel.State != ChannelState.Connecting) return true;
        IsConnected = false;
        return false;
    }

    public void StartServe(AsyncDuplexStreamingCall<StreamMessage, StreamMessage> duplexStreamingCall, Task serveTask, CancellationTokenSource tokenSource)
    {
        _duplexStreamingCall = duplexStreamingCall;
        _serveTask = serveTask;
        _streamListenTaskTokenSource = tokenSource;
    }

    public IAsyncStreamWriter<StreamMessage> GetResponseStream()
    {
        return _duplexStreamingCall?.RequestStream;
    }
}