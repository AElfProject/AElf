using System;
using System.Net;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class GrpcNetworkServer
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileConnecting(
        Exception ex, DnsEndPoint endpoint)
    {
        Logger.LogWarning(ex, $"Connect peer failed {endpoint.Host}: {endpoint.Port}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue,
        };
    }
}