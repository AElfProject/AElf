using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }

        bool IsFull();
        
        Task<bool> RemovePeerByAddressAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        IPeer GetBestPeer(); // todo move to service
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);

        bool TryAddPeer(IPeer peer);

        Task<IPeer> RemovePeerAsync(string remotePubKey, bool sendDisconnect);
    }
}