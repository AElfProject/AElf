using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<GrpcPeer> GetPeers();
        GrpcPeer FindPeer(string peerAddress, byte[] pubKey = null);
        
        bool AuthenticatePeer(string peerAddress, byte[] pubKey, Handshake handshake);
        bool AddPeer(GrpcPeer peer);
        void ProcessDisconnection(string peerEndpoint);
        Task<Handshake> GetHandshakeAsync();
    }
}