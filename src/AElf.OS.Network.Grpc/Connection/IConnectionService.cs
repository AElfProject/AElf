using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Grpc
{
    public interface IConnectionService
    {
        GrpcPeer GetPeerByPubkey(string pubkey);
        Task DisconnectAsync(IPeer peer, bool sendDisconnect = false);
        Task<bool> ConnectAsync(string ipAddress);
        Task<ConnectReply> DialBackAsync(string peerConnectionIp, ConnectionInfo peerConnectionInfo);
        Task<HandshakeReply> CheckIncomingHandshakeAsync(string peerId, Handshake handshake);
        Task DisconnectPeersAsync(bool gracefulDisconnect);
        void RemovePeer(string pubkey);
    }
}