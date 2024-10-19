using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.Network.Grpc.Helpers;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class StreamService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileProcessingStreamRequest(Exception ex,
        StreamMessage message, ServerCallContext context)
    {
        Logger.LogError(ex, "handle stream call failed, clientPubKey={clientPubKey} request={requestId} {streamType}-{messageType} latency={latency}",
            context.GetPublicKey(), message.RequestId, message.StreamType, message.MessageType, CommonHelper.GetRequestLatency(message.RequestId));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileProcessingStreamReply(Exception ex,
        ByteString reply, string clientPubKey)
    {
        var message = StreamMessage.Parser.ParseFrom(reply);
        Logger.LogError(ex, "handle stream call failed, clientPubKey={clientPubKey} request={requestId} {streamType}-{messageType} latency={latency}",
            clientPubKey, message.RequestId, message.StreamType, message.MessageType, CommonHelper.GetRequestLatency(message.RequestId));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileProcessingRequest(Exception ex,
        StreamMessage request, GrpcStreamPeer responsePeer, IStreamContext streamContext)
    {
        Logger.LogWarning(ex, "request failed {requestId} {streamType}-{messageType}", request.RequestId, request.StreamType, request.MessageType);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
}