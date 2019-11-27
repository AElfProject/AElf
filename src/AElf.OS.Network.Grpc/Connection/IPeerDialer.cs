using System;
using System.Net;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerDialer
    {
        Task<GrpcPeer> DialPeerAsync(DnsEndPoint remoteEndPoint);
        Task<GrpcPeer> DialBackPeerAsync(DnsEndPoint endpoint, Handshake handshake);
    }
}