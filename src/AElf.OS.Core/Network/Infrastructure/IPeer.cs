using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Metrics;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.Network.Infrastructure;

public interface IPeer
{
    bool IsReady { get; }
    string ConnectionStatus { get; }
    bool IsInvalid { get; }
    SyncState SyncState { get; }
    Hash LastKnownLibHash { get; }
    long LastKnownLibHeight { get; }
    Timestamp LastReceivedHandshakeTime { get; }
    DnsEndPoint RemoteEndpoint { get; }

    int BufferedTransactionsCount { get; }
    int BufferedBlocksCount { get; }
    int BufferedAnnouncementsCount { get; }

    byte[] InboundSessionId { get; }

    PeerConnectionInfo Info { get; }

    Task CheckHealthAsync();

    bool KnowsBlock(Hash hash);
    bool TryAddKnownBlock(Hash blockHash);
    bool KnowsTransaction(Hash hash);
    bool TryAddKnownTransaction(Hash transactionHash);
    void UpdateLastKnownLib(LibAnnouncement libAnnouncement);

    void EnqueueAnnouncement(BlockAnnouncement transaction, Action<NetworkException> sendCallback);
    void EnqueueTransaction(Transaction transaction, Action<NetworkException> sendCallback);
    void EnqueueBlock(BlockWithTransactions blockWithTransactions, Action<NetworkException> sendCallback);
    void EnqueueLibAnnouncement(LibAnnouncement libAnnouncement, Action<NetworkException> sendCallback);

    Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
    Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
    Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);

    Task<bool> TryRecoverAsync();

    Dictionary<string, List<RequestMetric>> GetRequestMetrics();
    Task DisconnectAsync(bool gracefulDisconnect);
}