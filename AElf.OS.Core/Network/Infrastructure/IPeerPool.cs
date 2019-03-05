using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<IPeer> GetPeers();

        //TODO: in two method, FindPeerByAddress, FindPeerByPubkey.
        IPeer FindPeer(string peerAddress, byte[] pubKey = null);

        //TODO: it seems it only cares about pubKey in Handshake?
        bool IsAuthenticatePeer(string peerAddress, Handshake handshake);

        bool AddPeer(IPeer peer);

        //TODO: is it equal to address?
        void ProcessDisconnection(string peerEndpoint);

        Task<Handshake> GetHandshakeAsync();
    }
}