using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerDialer
    {
        Task<GrpcPeer> DialPeerAsync(string ipAddress, ConnectionInfo connectionInfo);
        Task<GrpcPeer> DialBackPeer(string ipAddress, ConnectionInfo connectionInfo);
    }
}