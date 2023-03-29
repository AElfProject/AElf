using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Metrics;
using AElf.Types;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public interface IPeerHolder
{
    bool IsReady { get; }
    bool IsConnected { get; set; }
    string ConnectionStatus { get; }

    Task<NodeList> GetNodesAsync(NodesRequest nodesRequest, Metadata header, GrpcRequest request);
    Task CheckHealthAsync(Metadata header, GrpcRequest request);
    Task<BlockWithTransactions> RequestBlockAsync(BlockRequest blockRequest, Metadata header, GrpcRequest request);

    Task<BlockList> RequestBlocksAsync(BlocksRequest blockRequest, Metadata header, GrpcRequest request);

    Task DisconnectAsync(bool gracefulDisconnect);
    Task ConfirmHandshakeAsync(ConfirmHandshakeRequest confirmHandshakeRequest, Metadata header, GrpcRequest request);
    Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions);
    Task BroadcastAnnouncementBlockAsync(BlockAnnouncement header);
    Task BroadcastTransactionAsync(Transaction transaction);
    Task BroadcastLibAnnouncementAsync(LibAnnouncement libAnnouncement);
    Task Ping();

    Dictionary<string, List<RequestMetric>> GetRequestMetrics();
    Task<bool> TryRecoverAsync();

    NetworkException HandleRpcException(RpcException exception, string errorMessage);
}