using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerByAddressAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);

        bool AddPeer(IPeer peer);

        Task<IPeer> RemovePeerAsync(string remotePubKey, bool sendDisconnect);

        Task<Handshake> GetHandshakeAsync();

    }
}