using System.Net;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerDialer
    {
        Task<GrpcPeer> DialPeerAsync(IPEndPoint endpoint);
        Task<GrpcPeer> DialBackPeer(IPEndPoint endpoint, ConnectionInfo peerConnectionInfo);
    }
}