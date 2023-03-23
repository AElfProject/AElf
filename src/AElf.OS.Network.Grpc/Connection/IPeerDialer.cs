using System.Net;
using System.Threading.Tasks;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public interface IPeerDialer
{
    Task<GrpcPeer> DialPeerAsync(DnsEndPoint remoteEndpoin);
    Task<GrpcPeer> DialBackPeerAsync(DnsEndPoint remoteEndpoint, Handshake handshake);

    Task<GrpcPeer> DialBackPeerByStreamAsync(IAsyncStreamWriter<StreamMessage> responseStream, Handshake handshake);

    Task<bool> CheckEndpointAvailableAsync(DnsEndPoint remoteEndpoint);
}