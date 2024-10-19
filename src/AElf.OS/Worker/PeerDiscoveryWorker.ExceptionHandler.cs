using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.Network;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Worker;

public partial class PeerDiscoveryWorker
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileAddingPeer(Exception ex, NodeInfo node)
    {
        Logger.LogError(ex, $"Exception connecting to {node.Endpoint}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue,
        };
    }
}