using System.Collections.Generic;
using AElf.OS.Network.Grpc;

namespace AElf.OS.Network
{
    public class NetworkTestContext
    {
        // When mocking the ConnectionService (that replies with the peers handshake) use this
        // list to compare the generated handshakes with the state of the peer after the handshake
        // is finished.
        public Dictionary<string, Handshake> GeneratedHandshakes = new Dictionary<string, Handshake>();
        
        // When mocking the dialer, this list contains the mocks of all the peers.
        public List<GrpcPeer> DialedPeers { get; } = new List<GrpcPeer>();

        public void AddDialedPeer(GrpcPeer peer)
        {
            DialedPeers.Add(peer);
        }

        public bool AllPeersWhereCleaned()
        {
            foreach (var peer in DialedPeers)
            {
                if (!peer.IsShutdown)
                    return false;
            }

            return true;
        }
    }
}