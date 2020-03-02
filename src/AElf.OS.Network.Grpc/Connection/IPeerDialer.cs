using System.Net;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc.Connection
{
    public interface IPeerDialer
    {
        Task<GrpcPeer> DialPeerAsync(DnsEndPoint remoteEndpoint);
        Task<GrpcPeer> DialBackPeerAsync(DnsEndPoint remoteEndpoint, Handshake handshake);
    }
}