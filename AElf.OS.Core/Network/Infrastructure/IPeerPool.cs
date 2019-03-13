using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string publicKey);

        bool IsAuthenticatePeer(string remotePubKey);

        bool AddPeer(IPeer peer);

        Task ProcessDisconnection(string peerEndpoint);

        Task<Handshake> GetHandshakeAsync();
    }
}