using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerDialer
    {
        Task<GrpcPeer> DialPeerAsync(string ipAddress);
        Task<GrpcPeer> DialBackPeer(string ipAddress, ConnectionInfo peerConnectionInfo);
    }
}