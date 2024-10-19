using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class AuthInterceptor
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileUnaryServerHandler<TRequest, TResponse>(Exception ex,
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        Logger.LogError(ex, $"Auth interceptor error {context.Peer}, {context.Method}: ");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}