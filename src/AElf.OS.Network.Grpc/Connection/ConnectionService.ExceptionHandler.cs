using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc;

public partial class ConnectionService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileConfirmingHandshake(Exception ex, string peerInfo,
        GrpcPeer currentPeer)
    {
        Logger.LogDebug(ex, $"Confirm handshake error. Peer: {currentPeer.Info.Pubkey}.");
        _peerPool.RemovePeer(currentPeer.Info.Pubkey);
        await currentPeer.DisconnectAsync(false);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
}