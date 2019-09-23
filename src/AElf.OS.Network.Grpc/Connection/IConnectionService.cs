using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Grpc
{
    public interface IConnectionService
    {
        GrpcPeer GetPeerByPubkey(string pubkey);
        Task DisconnectAsync(IPeer peer, bool sendDisconnect = false, bool recover = true);
        Task<bool> ConnectAsync(IPEndPoint endpoint);
        Task<HandshakeReply> DoHandshakeAsync(IPEndPoint endpoint, Handshake handshake);
        void ConfirmHandshake(string peerPubkey);
        Task DisconnectPeersAsync(bool gracefulDisconnect);
        void RemovePeer(string pubkey);
    }
}