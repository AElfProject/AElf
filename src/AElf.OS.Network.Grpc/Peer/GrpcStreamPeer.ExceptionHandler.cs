using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.Network.Application;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class GrpcStreamPeer
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileWriting(Exception ex,
        StreamJob job)
    {
        var type = ex switch
        {
            InvalidOperationException => NetworkExceptionType.Unrecoverable,
            TimeoutException => NetworkExceptionType.PeerUnstable,
            _ => NetworkExceptionType.HandlerException
        };
        job.SendCallback?.Invoke(
            new NetworkException(
                $"{job.StreamMessage?.RequestId}{job.StreamMessage?.StreamType}-{job.StreamMessage?.MessageType} size={job.StreamMessage.ToByteArray().Length} queueCount={_sendStreamJobs.InputCount}",
                ex, type));
        await Task.Delay(StreamRecoveryWaitTime);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileWriting(RpcException ex,
        StreamJob job)
    {
        job.SendCallback?.Invoke(HandleRpcException(ex,
            $"Could not write to stream to {this}, queueCount={_sendStreamJobs.InputCount}"));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
}