using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.CrossChain.Grpc.Client;

public partial class GrpcCrossChainClient<TData, TClient>
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequesting(Exception ex)
    {
        IsConnected = false;
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new GrpcCrossChainRequestException(ex.Message, ex)
        };
    }
}

public partial class ClientForParentChain
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequestingChainInitializationData(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new GrpcCrossChainRequestException("Request initialization data failed.", ex)
        };
    }
}