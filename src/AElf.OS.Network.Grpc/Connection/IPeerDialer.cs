using System.Net;
using System.Threading.Tasks;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public interface IPeerDialer
{
    Task<GrpcPeer> DialPeerAsync(DnsEndPoint remoteEndpoint);
    Task<GrpcPeer> DialBackPeerAsync(DnsEndPoint remoteEndpoint, Handshake handshake);
    Task<GrpcPeer> DialBackPeerByStreamAsync(DnsEndPoint remoteEndPoint, IAsyncStreamWriter<StreamMessage> responseStream, Handshake handshake);
    Task<bool> CheckEndpointAvailableAsync(DnsEndPoint remoteEndpoint);
    Task<bool> BuildStreamForPeerAsync(GrpcStreamPeer streamPeer, AsyncDuplexStreamingCall<StreamMessage, StreamMessage> call=null);
}