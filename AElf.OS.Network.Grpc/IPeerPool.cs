using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerPool
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<GrpcPeer> GetPeers();
        GrpcPeer GetPeer(string address);
        
        bool AuthenticatePeer(string peer, Handshake handshake);
        bool IsAuthenticated(string peer);
        bool FinalizeAuth(GrpcPeer peer);
        void ProcessDisconnection(string peer);
        Task<Handshake> GetHandshakeAsync();
    }
}