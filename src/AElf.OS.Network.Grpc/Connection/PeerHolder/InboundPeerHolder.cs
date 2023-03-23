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
        return await _streamClient.GetNodesAsync(nodesRequest, AddPeerMeta(header), request);
    }

    public async Task CheckHealthAsync(Metadata header, GrpcRequest request)
    {
        await _streamClient.CheckHealthAsync(AddPeerMeta(header), request);
    }

    public async Task<BlockWithTransactions> RequestBlockAsync(BlockRequest blockRequest, Metadata header, GrpcRequest request)
    {
        return await _streamClient.RequestBlockAsync(blockRequest, AddPeerMeta(header), request);
    }

    public async Task<BlockList> RequestBlocksAsync(BlocksRequest blockRequest, Metadata header, GrpcRequest request)
    {
        return await _streamClient.RequestBlocksAsync(blockRequest, AddPeerMeta(header), request);
    }

    public async Task DisconnectAsync(bool gracefulDisconnect)
    {
        IsConnected = false;
        // send disconnect message if the peer is still connected and the connection
        // is stable.
        if (gracefulDisconnect)
        {
            var request = new GrpcRequest { ErrorMessage = "Could not send disconnect." };

            try
            {
                await _streamClient.DisconnectAsync(new DisconnectReason
                    { Why = DisconnectReason.Types.Reason.Shutdown }, AddPeerMeta(new Metadata { { GrpcConstants.SessionIdMetadataKey, Info.SessionId } }), request);
            }
            catch (NetworkException)
            {
                // swallow the exception, we don't care because we're disconnecting.
            }
        }
    }


    public async Task ConfirmHandshakeAsync(ConfirmHandshakeRequest confirmHandshakeRequest, Metadata header, GrpcRequest request)
    {
        await _streamClient.ConfirmHandshakeAsync(confirmHandshakeRequest, AddPeerMeta(header), request);
    }

    public async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions)
    {
        await _streamClient.BroadcastBlockAsync(blockWithTransactions, AddPeerMeta(null));
    }

    public async Task BroadcastAnnouncementBlockAsync(BlockAnnouncement header)
    {
        await _streamClient.BroadcastAnnouncementBlockAsync(header, AddPeerMeta(null));
    }

    public async Task BroadcastTransactionAsync(Transaction transaction)
    {
        await _streamClient.BroadcastTransactionAsync(transaction, AddPeerMeta(null));
    }

    public async Task BroadcastLibAnnouncementAsync(LibAnnouncement libAnnouncement)
    {
        await _streamClient.BroadcastLibAnnouncementAsync(libAnnouncement, AddPeerMeta(null));
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
        return new NetworkException(errorMessage, exception);
    }
}