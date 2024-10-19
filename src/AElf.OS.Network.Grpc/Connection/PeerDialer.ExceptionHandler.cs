using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.Network.Events;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class PeerDialer
{
    protected async Task<FlowBehavior> HandleExceptionWhilePerformingPingNode(Exception ex, DnsEndPoint remoteEndpoint,
        GrpcClient client)
    {
        Logger.LogWarning(ex, $"Could not ping peer {remoteEndpoint}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }

    protected async Task<FlowBehavior> HandleExceptionWhileCallingDoHandshake(Exception ex, GrpcClient client,
        DnsEndPoint remoteEndPoint,
        Handshake handshake)
    {
        await client.Channel.ShutdownAsync();
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }

    protected async Task<FlowBehavior> HandleExceptionWhileRetrievingServerCertificate(OperationCanceledException ex,
        DnsEndPoint remoteEndpoint)
    {
        Logger.LogDebug($"Certificate retrieval connection timeout for {remoteEndpoint}.");

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }

    protected async Task<FlowBehavior> HandleExceptionWhileRetrievingServerCertificate(Exception ex,
        DnsEndPoint remoteEndpoint)
    {
        // swallow exception because it's currently not a hard requirement to 
        // upgrade the connection.
        Logger.LogWarning(ex, $"Could not retrieve certificate from {remoteEndpoint}.");

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }

    protected async Task<FlowBehavior> HandleExceptionWhileDialingStreamPeer(Exception ex,
        GrpcClient client, DnsEndPoint remoteEndpoint, PeerConnectionInfo connectionInfo)
    {
        Logger.LogError(ex, "stream handle shake failed {remoteEndpoint}", remoteEndpoint);
        if (client.Channel.State == ChannelState.Idle || client.Channel.State == ChannelState.Ready)
            await client.Channel.ShutdownAsync();
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
    
    
    protected async Task<FlowBehavior> HandleExceptionWhileProcessingStreamMessages(Exception ex,
        IAsyncStreamReader<StreamMessage> responseStream, GrpcStreamPeer streamPeer)
    {
        if (ex is RpcException exception)
            await EventBus.PublishAsync(new StreamPeerExceptionEvent(streamPeer.HandleRpcException(exception, "listen err {remoteEndPoint}"), streamPeer));
        Logger.LogError(ex, "listen err {remoteEndPoint}", streamPeer.RemoteEndpoint.ToString());
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
    
    protected async Task<FlowBehavior> HandleExceptionWhilePingingNode(Exception ex,
        GrpcClient client, DnsEndPoint peerEndpoint)
    {
        Logger.LogWarning(ex, $"Could not ping {peerEndpoint}.");
        await client.Channel.ShutdownAsync();
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }

    protected async Task CloseClient(DnsEndPoint remoteEndpoint, TcpClient client = null)
    {
        client?.Close();
    }
}