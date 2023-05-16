using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Metrics;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

/// <summary>
///     Represents a connection to a peer.
/// </summary>
public class GrpcPeer : IPeer
{
    private const int MaxMetricsPerMethod = 100;
    protected const int BlockRequestTimeout = 700;
    protected const int CheckHealthTimeout = 1000;
    protected const int BlocksRequestTimeout = 5000;
    protected const int GetNodesTimeout = 500;
    protected const int UpdateHandshakeTimeout = 3000;
    protected const int StreamRecoveryWaitTime = 500;

    private const int BlockCacheMaxItems = 1024;
    private const int TransactionCacheMaxItems = 10_000;

    private const int QueuedTransactionTimeout = 10_000;
    private const int QueuedBlockTimeout = 100_000;

    protected readonly Channel _channel;
    protected readonly PeerService.PeerServiceClient _client;
    private readonly BoundedExpirationCache _knownBlockCache;

    private readonly BoundedExpirationCache _knownTransactionCache;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>> _recentRequestsRoundtripTimes;

    private readonly ActionBlock<StreamJob> _sendAnnouncementJobs;
    private readonly ActionBlock<StreamJob> _sendBlockJobs;
    private readonly ActionBlock<StreamJob> _sendTransactionJobs;
    private AsyncClientStreamingCall<BlockAnnouncement, VoidReply> _announcementStreamCall;
    private AsyncClientStreamingCall<BlockWithTransactions, VoidReply> _blockStreamCall;
    private AsyncClientStreamingCall<LibAnnouncement, VoidReply> _libAnnouncementStreamCall;

    private AsyncClientStreamingCall<Transaction, VoidReply> _transactionStreamCall;

    public GrpcPeer(GrpcClient client, DnsEndPoint remoteEndpoint, PeerConnectionInfo peerConnectionInfo)
    {
        _channel = client?.Channel;
        _client = client?.Client;

        RemoteEndpoint = remoteEndpoint;
        Info = peerConnectionInfo;

        _knownTransactionCache = new BoundedExpirationCache(TransactionCacheMaxItems, QueuedTransactionTimeout);
        _knownBlockCache = new BoundedExpirationCache(BlockCacheMaxItems, QueuedBlockTimeout);

        _recentRequestsRoundtripTimes = new ConcurrentDictionary<string, ConcurrentQueue<RequestMetric>>();
        RecentRequestsRoundtripTimes =
            new ReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>>(_recentRequestsRoundtripTimes);

        _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlock), new ConcurrentQueue<RequestMetric>());
        _recentRequestsRoundtripTimes.TryAdd(nameof(MetricNames.GetBlocks), new ConcurrentQueue<RequestMetric>());

        _sendAnnouncementJobs = new ActionBlock<StreamJob>(SendStreamJobAsync,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = NetworkConstants.DefaultMaxBufferedAnnouncementCount
            });
        _sendBlockJobs = new ActionBlock<StreamJob>(SendStreamJobAsync,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = NetworkConstants.DefaultMaxBufferedBlockCount
            });
        _sendTransactionJobs = new ActionBlock<StreamJob>(SendStreamJobAsync,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = NetworkConstants.DefaultMaxBufferedTransactionCount
            });
    }

    public Timestamp LastSentHandshakeTime { get; private set; }

    public bool IsConnected { get; set; }
    public bool IsShutdown { get; set; }
    public Hash CurrentBlockHash { get; private set; }
    public long CurrentBlockHeight { get; private set; }

    /// <summary>
    ///     Session ID to use when sending messages to this peer, announced at connection
    ///     from the other peer.
    /// </summary>
    public byte[] OutboundSessionId => Info.SessionId;

    public IReadOnlyDictionary<string, ConcurrentQueue<RequestMetric>> RecentRequestsRoundtripTimes { get; }

    /// <summary>
    ///     Property that describes that describes if the peer is ready for send/request operations. It's based
    ///     on the state of the underlying channel and the IsConnected.
    /// </summary>
    public bool IsReady => _channel != null ? (_channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready) && IsConnected : IsConnected;

    public bool IsInvalid =>
        !IsConnected &&
        Info.ConnectionTime.AddMilliseconds(NetworkConstants.PeerConnectionTimeout) <
        TimestampHelper.GetUtcNow();

    public virtual string ConnectionStatus => _channel != null ? _channel.State.ToString() : "";

    public Hash LastKnownLibHash { get; private set; }
    public long LastKnownLibHeight { get; private set; }
    public Timestamp LastReceivedHandshakeTime { get; private set; }
    public SyncState SyncState { get; set; }

    /// <summary>
    ///     Session ID to use when authenticating messages from this peer, announced to the
    ///     remote peer at connection.
    /// </summary>
    public byte[] InboundSessionId { get; set; }

    public DnsEndPoint RemoteEndpoint { get; }
    public int BufferedTransactionsCount => _sendTransactionJobs.InputCount;
    public int BufferedBlocksCount => _sendBlockJobs.InputCount;
    public int BufferedAnnouncementsCount => _sendAnnouncementJobs.InputCount;

    public PeerConnectionInfo Info { get; }

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

    public virtual Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest)
    {
        var request = new GrpcRequest { ErrorMessage = "Request nodes failed." };
        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, GetNodesTimeout.ToString() },
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };

        return RequestAsync(() => _client.GetNodesAsync(new NodesRequest { MaxCount = count }, data), request);
    }

    public void UpdateLastKnownLib(LibAnnouncement libAnnouncement)
    {
        if (libAnnouncement.LibHeight <= LastKnownLibHeight) return;

        LastKnownLibHash = libAnnouncement.LibHash;
        LastKnownLibHeight = libAnnouncement.LibHeight;
    }

    public virtual async Task CheckHealthAsync()
    {
        var request = new GrpcRequest { ErrorMessage = "Check health failed." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, CheckHealthTimeout.ToString() },
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };

        await RequestAsync(() => _client.CheckHealthAsync(new HealthCheckRequest(), data), request);
    }

    public virtual async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash)
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
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };

        var blockReply = await RequestAsync(() => _client.RequestBlockAsync(blockRequest, data), request);

        return blockReply?.Block;
    }

    public virtual async Task<List<BlockWithTransactions>> GetBlocksAsync(Hash firstHash, int count)
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
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };

        var list = await RequestAsync(() => _client.RequestBlocksAsync(blockRequest, data), request);

        if (list == null)
            return new List<BlockWithTransactions>();

        return list.Blocks.ToList();
    }

    public virtual async Task<bool> TryRecoverAsync()
    {
        if (_channel.State == ChannelState.Shutdown)
            return false;

        await _channel.TryWaitForStateChangedAsync(_channel.State,
            DateTime.UtcNow.AddSeconds(NetworkConstants.DefaultPeerRecoveryTimeout));

        // Either we connected again or the state change wait timed out.
        if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
        {
            IsConnected = false;
            return false;
        }

        return true;
    }

    public bool KnowsBlock(Hash hash)
    {
        return _knownBlockCache.HasHash(hash, false);
    }

    public bool TryAddKnownBlock(Hash blockHash)
    {
        return _knownBlockCache.TryAdd(blockHash);
    }

    public bool KnowsTransaction(Hash hash)
    {
        return _knownTransactionCache.HasHash(hash, false);
    }

    public bool TryAddKnownTransaction(Hash transactionHash)
    {
        return _knownTransactionCache.TryAdd(transactionHash);
    }

    public virtual async Task DisconnectAsync(bool gracefulDisconnect)
    {
        IsConnected = false;
        IsShutdown = true;

        // we complete but no need to await the jobs
        _sendAnnouncementJobs.Complete();
        _sendBlockJobs.Complete();
        _sendTransactionJobs.Complete();

        _announcementStreamCall?.Dispose();
        _transactionStreamCall?.Dispose();
        _blockStreamCall?.Dispose();

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

    public void UpdateLastReceivedHandshake(Handshake handshake)
    {
        LastKnownLibHeight = handshake.HandshakeData.LastIrreversibleBlockHeight;
        CurrentBlockHash = handshake.HandshakeData.BestChainHash;
        CurrentBlockHeight = handshake.HandshakeData.BestChainHeight;
        LastReceivedHandshakeTime = handshake.HandshakeData.Time;
    }

    public void UpdateLastSentHandshake(Handshake handshake)
    {
        LastSentHandshakeTime = handshake.HandshakeData.Time;
    }

    public virtual async Task ConfirmHandshakeAsync()
    {
        var request = new GrpcRequest { ErrorMessage = "Could not send confirm handshake." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, UpdateHandshakeTimeout.ToString() },
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };

        await RequestAsync(() => _client.ConfirmHandshakeAsync(new ConfirmHandshakeRequest(), data), request);
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

    protected virtual void RecordMetric(GrpcRequest grpcRequest, Timestamp requestStartTime, long elapsedMilliseconds)
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

    /// <summary>
    ///     This method handles the case where the peer is potentially down. If the Rpc call
    ///     put the channel in TransientFailure or Connecting, we give the connection a certain time to recover.
    /// </summary>
    protected virtual NetworkException HandleRpcException(RpcException exception, string errorMessage)
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
            // there was an exception, not related to connectivity.
            if (exception.StatusCode == StatusCode.Cancelled)
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

    public override string ToString()
    {
        return $"{{ listening-port: {RemoteEndpoint}, key: {Info.Pubkey.Substring(0, 45)}... }}";
    }

    protected enum MetricNames
    {
        GetBlocks,
        GetBlock
    }

    #region Streaming

    public void EnqueueTransaction(Transaction transaction, Action<NetworkException> sendCallback)
    {
        if (!IsReady)
            throw new NetworkException($"Dropping transaction, peer is not ready - {this}.",
                NetworkExceptionType.NotConnected);

        _sendTransactionJobs.Post(new StreamJob { Transaction = transaction, SendCallback = sendCallback });
    }

    public void EnqueueAnnouncement(BlockAnnouncement announcement, Action<NetworkException> sendCallback)
    {
        if (!IsReady)
            throw new NetworkException($"Dropping announcement, peer is not ready - {this}.",
                NetworkExceptionType.NotConnected);

        _sendAnnouncementJobs.Post(new StreamJob { BlockAnnouncement = announcement, SendCallback = sendCallback });
    }

    public void EnqueueBlock(BlockWithTransactions blockWithTransactions, Action<NetworkException> sendCallback)
    {
        if (!IsReady)
            throw new NetworkException($"Dropping block, peer is not ready - {this}.",
                NetworkExceptionType.NotConnected);

        _sendBlockJobs.Post(
            new StreamJob { BlockWithTransactions = blockWithTransactions, SendCallback = sendCallback });
    }

    public void EnqueueLibAnnouncement(LibAnnouncement libAnnouncement, Action<NetworkException> sendCallback)
    {
        if (!IsReady)
            throw new NetworkException($"Dropping lib announcement, peer is not ready - {this}.",
                NetworkExceptionType.NotConnected);

        _sendAnnouncementJobs.Post(new StreamJob
        {
            LibAnnouncement = libAnnouncement,
            SendCallback = sendCallback
        });
    }

    private async Task SendStreamJobAsync(StreamJob job)
    {
        if (!IsReady)
            return;

        try
        {
            if (job.Transaction != null)
                await SendTransactionAsync(job.Transaction);
            else if (job.BlockAnnouncement != null)
                await SendAnnouncementAsync(job.BlockAnnouncement);
            else if (job.BlockWithTransactions != null)
                await BroadcastBlockAsync(job.BlockWithTransactions);
            else if (job.LibAnnouncement != null) await SendLibAnnouncementAsync(job.LibAnnouncement);
        }
        catch (RpcException ex)
        {
            job.SendCallback?.Invoke(HandleRpcException(ex, $"Could not broadcast to {this}: "));
            await Task.Delay(StreamRecoveryWaitTime);
            return;
        }
        catch (Exception ex)
        {
            job.SendCallback?.Invoke(new NetworkException("Unknown exception during broadcast.", ex));
            throw;
        }

        job.SendCallback?.Invoke(null);
    }

    protected virtual async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        if (_blockStreamCall == null)
            _blockStreamCall = _client.BlockBroadcastStream(new Metadata
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

    /// <summary>
    ///     Send a announcement to the peer using the stream call.
    ///     Note: this method is not thread safe.
    /// </summary>
    protected virtual async Task SendAnnouncementAsync(BlockAnnouncement header)
    {
        if (_announcementStreamCall == null)
            _announcementStreamCall = _client.AnnouncementBroadcastStream(new Metadata
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

    /// <summary>
    ///     Send a transaction to the peer using the stream call.
    ///     Note: this method is not thread safe.
    /// </summary>
    protected virtual async Task SendTransactionAsync(Transaction transaction)
    {
        if (_transactionStreamCall == null)
            _transactionStreamCall = _client.TransactionBroadcastStream(new Metadata
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

    /// <summary>
    ///     Send a lib announcement to the peer using the stream call.
    ///     Note: this method is not thread safe.
    /// </summary>
    public virtual async Task SendLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        if (_libAnnouncementStreamCall == null)
            _libAnnouncementStreamCall = _client.LibAnnouncementBroadcastStream(new Metadata
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

    #endregion
}