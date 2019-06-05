using AElf.Kernel;
using AElf.OS.Network.Grpc;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network
{
    public static class GrpcTestHelper
    {
        public static GrpcPeer CreateNewPeer(string ipAddress = "127.0.0.1:2000", bool isValid = true)
        {
            var channel = new Channel(ipAddress, ChannelCredentials.Insecure);
            
            PeerService.PeerServiceClient client;
            
            if(isValid)
                client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
                {
                    metadata.Add(GrpcConstants.PubkeyMetadataKey, GrpcTestConstants.FakePubKey);
                    return metadata;
                }));
            else
                client = new PeerService.PeerServiceClient(channel);
            
            var connectionInfo = new GrpcPeerInfo
            {
                PublicKey = GrpcTestConstants.FakePubKey,
                PeerIpAddress = ipAddress,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = 1,
                IsInbound = true
            };

            return new GrpcPeer(channel, client, connectionInfo);
        }
    }
}