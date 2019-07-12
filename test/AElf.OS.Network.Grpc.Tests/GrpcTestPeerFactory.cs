using AElf.Kernel;
using AElf.OS.Network.Grpc;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Moq;

namespace AElf.OS.Network
{
    public static class GrpcTestPeerFactory
    {
        private static Channel CreateMockChannel() => new Channel("127.0.0.1:9999", ChannelCredentials.Insecure);
        
        public static GrpcPeer CreateBasicPeer(string ip, string pubkey)
        {
            return CreatePeerWithInfo(ip, new PeerInfo { Pubkey = pubkey });
        }

        public static GrpcPeer CreatePeerWithInfo(string ip, PeerInfo info)
        {
            return new GrpcPeer(CreateMockChannel(), null, ip, info);
        }

        public static GrpcPeer CreatePeerWithClient(string ip, string pubkey, PeerService.PeerServiceClient client)
        {
            return new GrpcPeer(CreateMockChannel(), client, ip, new PeerInfo { Pubkey = pubkey });
        }
        
        public static GrpcPeer CreateNewPeer(string ipAddress = "127.0.0.1:2000", bool isValid = true)
        {
            var channel = new Channel(ipAddress, ChannelCredentials.Insecure);
            
            PeerService.PeerServiceClient client;
            
            if(isValid)
                client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
                {
                    metadata.Add(GrpcConstants.PubkeyMetadataKey, GrpcTestConstants.FakePubkey);
                    return metadata;
                }));
            else
                client = new PeerService.PeerServiceClient(channel);
            
            var connectionInfo = new PeerInfo
            {
                Pubkey = GrpcTestConstants.FakePubkey,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                IsInbound = true
            };

            return new GrpcPeer(channel, client, ipAddress, connectionInfo);
        }
    }
}