using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public partial class RetryInterceptor
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingResponse(OperationCanceledException ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new RpcException(new Status(StatusCode.Cancelled, "The server is not responding."))
        };
    }
}