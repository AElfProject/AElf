using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }

        bool IsFull();

        bool AddHandshakingPeer(IPEndPoint endpoint, Handshake handshake);
        bool RemoveHandshakingPeer(IPEndPoint endpoint, Handshake handshake);
        
        List<IPeer> GetPeers(bool includeFailing = false);

        IPeer FindPeerByEndpoint(IPEndPoint peerEndpoint);
        IPeer FindPeerByPublicKey(string remotePubKey);

        List<IPeer> GetPeersByIpAddress(IPAddress ipAddress);

        bool TryAddPeer(IPeer peer);
        IPeer RemovePeer(string publicKey);
    }
}