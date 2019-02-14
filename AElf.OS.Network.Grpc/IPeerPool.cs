using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<GrpcPeer> GetPeers();
        GrpcPeer FindPeer(string address, byte[] pubKey = null);
        
        bool AuthenticatePeer(string peerEndpoint, byte[] pubKey, Handshake handshake);
        bool IsAuthenticated(string peer);
        bool AddPeer(GrpcPeer peer);
        void ProcessDisconnection(string peer);
        Task<Handshake> GetHandshakeAsync();
    }
}