using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class GrpcRequestProcessor
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingBlock(Exception ex, BlockRequest request,
        string peerInfo, string peerPubkey, string requestId)
    {
        Logger.LogWarning(ex, $"Request block error: {peerPubkey}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingBlocks(Exception ex, BlocksRequest request,
        string peerInfo, string requestId)
    {
        Logger.LogWarning(ex, "Request blocks error - {peerInfo} - request={request}. requestId={requestId} ", peerInfo,
            request, requestId);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingNodes(Exception ex, NodesRequest request,
        string peerInfo)
    {
        Logger.LogWarning(ex, "Get nodes error: ");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileConfirmingHandshake(Exception ex, string peerInfo,
        string peerPubkey, string requestId)
    {
        Logger.LogWarning(ex, "Confirm handshake error - {peerInfo}: requestId={requestId}", peerInfo, requestId);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRemovingPeer(Exception ex, DisconnectReason request, string peerInfo, string peerPubkey, string requestId)
    {
        Logger.LogError(ex, "requestId={requestId}, Disconnect error: ", requestId);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
}