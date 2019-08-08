using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcClient
    {
        public Channel Channel { get; }
        public PeerService.PeerServiceClient Client { get; }

        public GrpcClient(Channel channel, PeerService.PeerServiceClient client)
        {
            Channel = channel;
            Client = client;
        }
    }
}