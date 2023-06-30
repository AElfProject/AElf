using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Events;

public class StreamPeerExceptionEvent
{
    public NetworkException Exception { get; }
    public IPeer Peer { get; }

    public StreamPeerExceptionEvent(NetworkException exception, IPeer peer)
    {
        Exception = exception;
        Peer = peer;
    }
}