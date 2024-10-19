using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.Network.Application;
using AElf.Types;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public partial class GrpcPeer
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileWriting(RpcException ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new NetworkException("Peer is closed", ex, NetworkExceptionType.Unrecoverable)
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileWriting<TResp>(AggregateException ex,
        Func<AsyncUnaryCall<TResp>> func, GrpcRequest requestParams)
    {
        if (!(ex.InnerException is RpcException rpcException))
            throw new NetworkException($"Unknown exception. {this}: {requestParams.ErrorMessage}",
                NetworkExceptionType.Unrecoverable);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = HandleRpcException(rpcException, requestParams.ErrorMessage)
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileSending(Exception ex,
        StreamJob job)
    {
        if (ex is RpcException rpcEx)
        {
            job.SendCallback?.Invoke(HandleRpcException(rpcEx, $"Could not broadcast to {this}: "));
            await Task.Delay(StreamRecoveryWaitTime);
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            };
        }

        job.SendCallback?.Invoke(new NetworkException("Unknown exception during broadcast.", ex));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileBroadcastingBlock(RpcException ex,
        BlockWithTransactions blockWithTransactions)
    {
        _blockStreamCall.Dispose();
        _blockStreamCall = null;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileSendingAnnouncement(RpcException ex,
        BlockAnnouncement header)
    {
        _announcementStreamCall.Dispose();
        _announcementStreamCall = null;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileSendingTransaction(RpcException ex,
        Transaction transaction)
    {
        _transactionStreamCall.Dispose();
        _transactionStreamCall = null;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileSendingLibAnnouncement(RpcException ex,
        Transaction transaction)
    {
        _libAnnouncementStreamCall.Dispose();
        _libAnnouncementStreamCall = null;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}