using AElf.Kernel;
using AElf.OS.Network.Grpc;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network
{
    public static class GrpcTestHelper
    {
        public static GrpcPeer CreateBasicPeer(string ip, string pubkey)
        {
            return CreatePeerWithInfo(ip, new PeerInfo { Pubkey = pubkey });
        }

        public static GrpcPeer CreatePeerWithInfo(string ip, PeerInfo info)
        {
            return new GrpcPeer(null, null, null, ip, info);
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
                StartHeight = 1,
                IsInbound = true
            };

            return new GrpcPeer(channel, client, null, ipAddress, connectionInfo);
        }
    }
}