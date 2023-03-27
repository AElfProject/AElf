using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Metrics;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public class InboundPeerHolder : IPeerHolder
{
    private readonly StreamClient _streamClient;
    private readonly Dictionary<string, string> _peerMeta;
    public PeerConnectionInfo Info { get; }

    public bool IsConnected { get; set; }
    public bool IsReady { get; }
    public string ConnectionStatus { get; }

    public InboundPeerHolder(StreamClient streamClient, PeerConnectionInfo info, Dictionary<string, string> peerMeta)
    {
        _streamClient = streamClient;
        Info = info;
        _peerMeta = peerMeta;
    }

    private Metadata AddPeerMeta(Metadata metadata)
    {
        metadata ??= new Metadata();
        foreach (var kv in _peerMeta)
        {
            metadata.Add(kv.Key, kv.Value);
        }

        return metadata;
    }

    public async Task<NodeList> GetNodesAsync(NodesRequest nodesRequest, Metadata header, GrpcRequest request)
    {
        try
        {
            return await _streamClient.GetNodesAsync(nodesRequest, AddPeerMeta(header));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, request.ErrorMessage);
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);

            throw;
        }
    }

    public async Task CheckHealthAsync(Metadata header, GrpcRequest request)
    {
        try
        {
            await _streamClient.CheckHealthAsync(AddPeerMeta(header));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, request.ErrorMessage);
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);

            throw;
        }
    }

    public async Task<BlockWithTransactions> RequestBlockAsync(BlockRequest blockRequest, Metadata header, GrpcRequest request)
    {
        try
        {
            return await _streamClient.RequestBlockAsync(blockRequest, AddPeerMeta(header));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, request.ErrorMessage);
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);

            throw;
        }
    }

    public async Task<BlockList> RequestBlocksAsync(BlocksRequest blockRequest, Metadata header, GrpcRequest request)
    {
        try
        {
            return await _streamClient.RequestBlocksAsync(blockRequest, AddPeerMeta(header));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, request.ErrorMessage);
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
            throw;
        }
    }

    public async Task DisconnectAsync(bool gracefulDisconnect)
    {
        if (!IsConnected) return;
        IsConnected = false;
        // send disconnect message if the peer is still connected and the connection
        // is stable.
        if (!gracefulDisconnect) return;
        var request = new GrpcRequest { ErrorMessage = "Could not send disconnect." };

        try
        {
            await _streamClient.DisconnectAsync(new DisconnectReason
                { Why = DisconnectReason.Types.Reason.Shutdown }, AddPeerMeta(new Metadata { { GrpcConstants.SessionIdMetadataKey, Info.SessionId } }));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, request.ErrorMessage);
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
        }
    }


    public async Task ConfirmHandshakeAsync(ConfirmHandshakeRequest confirmHandshakeRequest, Metadata header, GrpcRequest request)
    {
        try
        {
            await _streamClient.ConfirmHandshakeAsync(confirmHandshakeRequest, AddPeerMeta(header));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, request.ErrorMessage);
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
            throw;
        }
    }

    public async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        try
        {
            await _streamClient.BroadcastBlockAsync(blockWithTransactions, AddPeerMeta(null));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, "BroadcastBlockAsync failed");
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
            throw;
        }
    }

    public async Task BroadcastAnnouncementBlockAsync(BlockAnnouncement header)
    {
        try
        {
            await _streamClient.BroadcastAnnouncementBlockAsync(header, AddPeerMeta(null));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, "BroadcastAnnouncementBlockAsync failed");
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
            throw;
        }
    }

    public async Task BroadcastTransactionAsync(Transaction transaction)
    {
        try
        {
            await _streamClient.BroadcastTransactionAsync(transaction, AddPeerMeta(null));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, "BroadcastTransactionAsync failed");
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
            throw;
        }
    }

    public async Task BroadcastLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        try
        {
            await _streamClient.BroadcastLibAnnouncementAsync(libAnnouncement, AddPeerMeta(null));
        }
        catch (RpcException e)
        {
            var networkException = HandleRpcException(e, "BroadcastLibAnnouncementAsync failed");
            if (networkException.ExceptionType == NetworkExceptionType.Unrecoverable)
                DisconnectAsync(true);
            throw;
        }
    }

    public Dictionary<string, List<RequestMetric>> GetRequestMetrics()
    {
        return null;
    }

    public async Task<bool> TryRecoverAsync()
    {
        IsConnected = false;
        return IsConnected;
    }

    public NetworkException HandleRpcException(RpcException exception, string errorMessage)
    {
        var message = $"Failed request to {this}: {errorMessage}";
        var type = NetworkExceptionType.Rpc;
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

        return new NetworkException(message, exception, type);
    }
}