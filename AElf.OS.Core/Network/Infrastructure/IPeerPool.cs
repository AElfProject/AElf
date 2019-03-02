using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<IPeer> GetPeers();
        IPeer FindPeer(string peerAddress, byte[] pubKey = null);
        
        bool AuthenticatePeer(string peerAddress, Handshake handshake);
        bool AddPeer(IPeer peer);
        void ProcessDisconnection(string peerEndpoint);

        Task<Handshake> GetHandshakeAsync();
    }
}