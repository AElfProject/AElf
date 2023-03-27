using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.Network.Grpc;

/// <summary>
///     Represents a connection to a peer.
/// </summary>
public class GrpcPeer : IPeer
{
    private const int BlockRequestTimeout = 700;
    private const int CheckHealthTimeout = 1000;
    private const int BlocksRequestTimeout = 5000;
    private const int GetNodesTimeout = 500;
    private const int UpdateHandshakeTimeout = 3000;
    private const int BlockCacheMaxItems = 1024;
    private const int TransactionCacheMaxItems = 10_000;

    private const int QueuedTransactionTimeout = 10_000;
    private const int QueuedBlockTimeout = 100_000;

    public IPeerHolder Holder { get; }
    private readonly BoundedExpirationCache _knownBlockCache;

    private readonly BoundedExpirationCache _knownTransactionCache;

    private readonly ActionBlock<StreamJob> _sendAnnouncementJobs;
    private readonly ActionBlock<StreamJob> _sendBlockJobs;
    private readonly ActionBlock<StreamJob> _sendTransactionJobs;
    public ILogger<GrpcPeer> Logger { get; set; }

    public GrpcPeer(IPeerHolder holder, DnsEndPoint remoteEndpoint, PeerConnectionInfo peerConnectionInfo)
    {
        Holder = holder;
        RemoteEndpoint = remoteEndpoint;
        Info = peerConnectionInfo;

        _knownTransactionCache = new BoundedExpirationCache(TransactionCacheMaxItems, QueuedTransactionTimeout);
        _knownBlockCache = new BoundedExpirationCache(BlockCacheMaxItems, QueuedBlockTimeout);

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
        Logger = NullLogger<GrpcPeer>.Instance;
    }

    public Timestamp LastSentHandshakeTime { get; private set; }


    public bool IsShutdown { get; set; }
    public Hash CurrentBlockHash { get; private set; }
    public long CurrentBlockHeight { get; private set; }

    /// <summary>
    ///     Session ID to use when sending messages to this peer, announced at connection
    ///     from the other peer.
    /// </summary>
    public byte[] OutboundSessionId => Info.SessionId;

    /// <summary>
    ///     Property that describes that describes if the peer is ready for send/request operations. It's based
    ///     on the state of the underlying channel and the IsConnected.
    /// </summary>
    public bool IsReady => Holder.IsReady;

    public bool IsConnected => Holder.IsConnected;

    public bool IsInvalid =>
        !Holder.IsConnected &&
        Info.ConnectionTime.AddMilliseconds(NetworkConstants.PeerConnectionTimeout) <
        TimestampHelper.GetUtcNow();

    public string ConnectionStatus => Holder.ConnectionStatus;

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
        return Holder.GetRequestMetrics();
    }

    public Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest)
    {
        var request = new GrpcRequest { ErrorMessage = "Request nodes failed." };
        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, GetNodesTimeout.ToString() },
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };

        return Holder.GetNodesAsync(new NodesRequest { MaxCount = count }, data, request);
    }

    public void UpdateLastKnownLib(LibAnnouncement libAnnouncement)
    {
        if (libAnnouncement.LibHeight <= LastKnownLibHeight) return;

        LastKnownLibHash = libAnnouncement.LibHash;
        LastKnownLibHeight = libAnnouncement.LibHeight;
    }

    public async Task CheckHealthAsync()
    {
        var request = new GrpcRequest { ErrorMessage = "Check health failed." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, CheckHealthTimeout.ToString() },
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };
        await Holder.CheckHealthAsync(data, request);
    }

    public async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash)
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

        return await Holder.RequestBlockAsync(blockRequest, data, request);
    }

    public async Task<List<BlockWithTransactions>> GetBlocksAsync(Hash firstHash, int count)
    {
        var blocksRequest = new BlocksRequest { PreviousBlockHash = firstHash, Count = count };
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

        var list = await Holder.RequestBlocksAsync(blocksRequest, data, request);
        return list == null ? new List<BlockWithTransactions>() : list.Blocks.ToList();
    }

    public async Task<bool> TryRecoverAsync()
    {
        return await Holder.TryRecoverAsync();
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

    public async Task DisconnectAsync(bool gracefulDisconnect)
    {
        Logger.LogWarning("disconnect {pubkey}", Info.Pubkey);
        IsShutdown = true;

        // we complete but no need to await the jobs
        _sendAnnouncementJobs.Complete();
        _sendBlockJobs.Complete();
        _sendTransactionJobs.Complete();
        await Holder.DisconnectAsync(gracefulDisconnect);
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

    public async Task ConfirmHandshakeAsync()
    {
        var request = new GrpcRequest { ErrorMessage = "Could not send confirm handshake." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, UpdateHandshakeTimeout.ToString() },
            { GrpcConstants.SessionIdMetadataKey, OutboundSessionId }
        };
        await Holder.ConfirmHandshakeAsync(new ConfirmHandshakeRequest(), data, request);
    }

    public override string ToString()
    {
        return $"{{ listening-port: {RemoteEndpoint}, {Info} }}";
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
            job.SendCallback?.Invoke(Holder.HandleRpcException(ex, $"Could not broadcast to {this}: "));
            await Task.Delay(GrpcConstants.StreamRecoveryWaitTime);
            return;
        }
        catch (Exception ex)
        {
            job.SendCallback?.Invoke(new NetworkException("Unknown exception during broadcast.", ex));
            throw;
        }

        job.SendCallback?.Invoke(null);
    }

    private async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        await Holder.BroadcastBlockAsync(blockWithTransactions);
    }

    /// <summary>
    ///     Send a announcement to the peer using the stream call.
    ///     Note: this method is not thread safe.
    /// </summary>
    private async Task SendAnnouncementAsync(BlockAnnouncement header)
    {
        await Holder.BroadcastAnnouncementBlockAsync(header);
    }

    /// <summary>
    ///     Send a transaction to the peer using the stream call.
    ///     Note: this method is not thread safe.
    /// </summary>
    private async Task SendTransactionAsync(Transaction transaction)
    {
        await Holder.BroadcastTransactionAsync(transaction);
    }

    /// <summary>
    ///     Send a lib announcement to the peer using the stream call.
    ///     Note: this method is not thread safe.
    /// </summary>
    public async Task SendLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        await Holder.BroadcastLibAnnouncementAsync(libAnnouncement);
    }

    #endregion
}

public enum MetricNames
{
    GetBlocks,
    GetBlock
}