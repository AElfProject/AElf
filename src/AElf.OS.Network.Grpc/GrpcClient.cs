using Grpc.Core;
using Org.BouncyCastle.X509;

namespace AElf.OS.Network.Grpc
{
    public class GrpcClient
    {
        public X509Certificate Certificate { get; }
        public Channel Channel { get; }
        public PeerService.PeerServiceClient Client { get; }

        public GrpcClient(Channel channel, PeerService.PeerServiceClient client, X509Certificate certificate = null)
        {
            Channel = channel;
            Client = client;
            Certificate = certificate;
        }
    }
}