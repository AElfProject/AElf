using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class GrpcServerService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileDoingHandshake(Exception ex,
        HandshakeRequest request, ServerCallContext context)
    {
        Logger.LogWarning(ex, $"Handshake failed - {context.Peer}: ");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequesting(Exception ex,
        IAsyncStreamReader<StreamMessage> requestStream, IServerStreamWriter<StreamMessage> responseStream,
        ServerCallContext context)
    {
        Logger.LogError(ex, "RequestByStream error - {peer}: ", context.Peer);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileBroadcastingBlock(Exception ex,
        IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
    {
        Logger.LogError(ex, $"Block stream error - {context.GetPeerInfo()}: ");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileBroadcastingAnnouncement(Exception ex,
        IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
    {
        Logger.LogError(ex, $"Announcement stream error: {context.GetPeerInfo()}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileBroadcastingTransaction(Exception ex,
        IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
    {
        Logger.LogError(ex, $"Transaction stream error - {context.GetPeerInfo()}: ");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileBroadcastingLib(Exception ex,
        IAsyncStreamReader<BlockWithTransactions> requestStream, ServerCallContext context)
    {
        Logger.LogError(ex, $"Lib announcement stream error: {context.GetPeerInfo()}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequestingBlocks(Exception ex,
        BlocksRequest request, ServerCallContext context)
    {
        Logger.LogWarning(ex, "Request blocks error - {peerInfo} - request={request}. ", context.GetPeerInfo(), request);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
}