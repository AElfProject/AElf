using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Application;

public partial class NetworkService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileEnqueuingAnnouncement(NetworkException ex,
        IPeer peer, Hash blockHash, BlockAnnouncement blockAnnouncement)
    {
        Logger.LogWarning(ex, $"Could not enqueue announcement to {peer} " +
                              $"- status {peer.ConnectionStatus}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileEnqueuingTransaction(NetworkException ex,
        Transaction transaction, IPeer peer, Hash txHash)
    {
        Logger.LogWarning(ex, $"Could not enqueue transaction to {peer} - " +
                              $"status {peer.ConnectionStatus}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileEnqueuingLibAnnouncement(NetworkException ex,
        IPeer peer, LibAnnouncement announce)
    {
        Logger.LogWarning(ex, $"Could not enqueue lib announcement to {peer} " +
                              $"- status {peer.ConnectionStatus}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileCheckingHealth(NetworkException ex,
        IPeer peer)
    {
        if (ex.ExceptionType == NetworkExceptionType.Unrecoverable
            || ex.ExceptionType == NetworkExceptionType.PeerUnstable)
        {
            Logger.LogInformation(ex, $"Removing unhealthy peer {peer}.");
            await _networkServer.TrySchedulePeerReconnectionAsync(peer);
        }
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileEnqueuingBlock(NetworkException ex,
        IPeer peer, BlockWithTransactions blockWithTransactions)
    {
        Logger.LogWarning(ex, $"Could not enqueue block to {peer} - status {peer.ConnectionStatus}.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequesting<T>(NetworkException ex,
        IPeer peer, Func<IPeer, Task<T>> func) where T : class
    {
        Logger.LogWarning(ex, $"Request failed from {peer.RemoteEndpoint}.");

        if (ex.ExceptionType == NetworkExceptionType.HandlerException)
        {
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
                ReturnValue = new Response<T>(default)
            };
        }

        await HandleNetworkExceptionAsync(peer, ex);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new Response<T>()
        };
    }
    
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingNodes(Exception ex,
        IPeer peer)
    {
        Logger.LogWarning(ex, $"Request failed from {peer.RemoteEndpoint}.");
        if (ex is NetworkException exception) await HandleNetworkExceptionAsync(peer, exception);
        Logger.LogWarning(ex, "get nodes failed. peer={peer}", peer);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new List<NodeInfo>()
        };
    }
}