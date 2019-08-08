using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerDialer
    {
        Task<GrpcPeer> DialPeerAsync(string ipAddress);
        Task<GrpcPeer> DialBackPeerAsync(string ipAddress, Handshake handshake);
    }
}