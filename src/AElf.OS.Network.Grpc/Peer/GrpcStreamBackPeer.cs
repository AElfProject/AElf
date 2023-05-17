using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public class GrpcStreamBackPeer : GrpcStreamPeer
{
    public GrpcStreamBackPeer(DnsEndPoint remoteEndpoint, PeerConnectionInfo peerConnectionInfo,
        IAsyncStreamWriter<StreamMessage> clientStreamWriter, IStreamTaskResourcePool streamTaskResourcePool,
        Dictionary<string, string> peerMeta)
        : base(null, remoteEndpoint, peerConnectionInfo,
            null, clientStreamWriter, streamTaskResourcePool, peerMeta)
    {
    }

    public override string ConnectionStatus => IsConnected ? "Stream Ready" : "Stream Closed";

    public override async Task CheckHealthAsync()
    {
        var request = new GrpcRequest { ErrorMessage = "Check health failed." };

        var data = new Metadata
        {
            { GrpcConstants.TimeoutMetadataKey, CheckHealthTimeout.ToString() },
        };
        await RequestAsync(() => StreamRequestAsync(MessageType.HealthCheck, new HealthCheckRequest(), data), request);
    }

    public override async Task DisconnectAsync(bool gracefulDisconnect)
    {
        if (!IsConnected) return;
        IsConnected = false;
        _sendStreamJobs.Complete();
        // send disconnect message if the peer is still connected and the connection
        // is stable.
        try
        {
            await RequestAsync(() => StreamRequestAsync(MessageType.Disconnect,
                    new DisconnectReason { Why = DisconnectReason.Types.Reason.Shutdown },
                    new Metadata { { GrpcConstants.SessionIdMetadataKey, OutboundSessionId } }),
                new GrpcRequest { ErrorMessage = "Could not send disconnect." });
        }
        catch (Exception)
        {
            // swallow the exception, we don't care because we're disconnecting.
        }
    }

    public override Task<bool> TryRecoverAsync()
    {
        return Task.FromResult(true);
    }


    protected override NetworkException HandleRpcException(RpcException exception, string errorMessage)
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
            type = NetworkExceptionType.Unrecoverable;
        }

        return new NetworkException(message, exception, type);
    }
}