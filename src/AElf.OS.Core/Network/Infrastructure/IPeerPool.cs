using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerByAddressAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);

        bool IsAuthenticatePeer(string remotePubKey);

        bool AddPeer(IPeer peer);

        Task<IPeer> RemovePeerAsync(string remotePubKey, bool sendDisconnect);

        Task<Handshake> GetHandshakeAsync();

    }
}