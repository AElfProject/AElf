using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }

        bool IsFull();
        
        List<IPeer> GetPeers(bool includeFailing = false);

        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);

        bool TryAddPeer(IPeer peer);
        IPeer RemovePeer(string publicKey);
    }
}