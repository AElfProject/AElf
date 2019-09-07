using AElf.Kernel;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network
{
    public static class GrpcTestPeerHelpers
    {
        private static Channel CreateMockChannel() => new Channel("127.0.0.1:9999", ChannelCredentials.Insecure);
        
        public static GrpcPeer CreateBasicPeer(string ip, string pubkey)
        {
            
            return CreatePeerWithInfo(ip, new PeerConnectionInfo { Pubkey = pubkey });
        }

        public static GrpcPeer CreatePeerWithInfo(string ip, PeerConnectionInfo info)
        {
            return new GrpcPeer(new GrpcClient(CreateMockChannel(), null), IpEndpointHelper.Parse(ip), info);
        }

        public static GrpcPeer CreatePeerWithClient(string ip, string pubkey, PeerService.PeerServiceClient client)
        {
            return new GrpcPeer(new GrpcClient(CreateMockChannel(), client), IpEndpointHelper.Parse(ip), new PeerConnectionInfo { Pubkey = pubkey });
        }
        
        public static GrpcPeer CreateNewPeer(string ipAddress = "127.0.0.1:2000", bool isValid = true, string publicKey = null)
        {
            var pubkey = publicKey ?? NetworkTestConstants.FakePubkey;
            var channel = new Channel(ipAddress, ChannelCredentials.Insecure);
            
            PeerService.PeerServiceClient client;
            
            if(isValid)
                client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
                {
                    metadata.Add(GrpcConstants.PubkeyMetadataKey, pubkey);
                    return metadata;
                }));
            else
                client = new PeerService.PeerServiceClient(channel);
            
            var connectionInfo = new PeerConnectionInfo
            {
                Pubkey = pubkey,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                IsInbound = true
            };

            return new GrpcPeer(new GrpcClient(channel, client), IpEndpointHelper.Parse(ipAddress), connectionInfo);
        }
    }
}