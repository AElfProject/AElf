using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Events
{
    public class PeerDisconnectionEvent
    {
        public IPeer Peer { get; }

        public PeerDisconnectionEvent(IPeer peer)
        {
            Peer = peer;
        }
    }
}