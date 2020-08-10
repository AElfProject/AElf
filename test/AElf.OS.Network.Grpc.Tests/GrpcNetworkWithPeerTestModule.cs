using System;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    [DependsOn(typeof(GrpcNetworkBaseTestModule))]
    public class GrpcNetworkWithPeerTestModule : AElfModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var channel = new Channel(NetworkTestConstants.FakeIpEndpoint, ChannelCredentials.Insecure);
            
            var connectionInfo = new PeerConnectionInfo
            {
                Pubkey = NetworkTestConstants.FakePubkey2,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow(),
                IsInbound = true
            };
            
            if (!AElfPeerEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out var peerEndpoint))
                throw new Exception($"Ip {NetworkTestConstants.FakeIpEndpoint} is invalid.");
            
            pool.TryAddPeer(new GrpcPeer(new GrpcClient(channel, new PeerService.PeerServiceClient(channel)), peerEndpoint, connectionInfo));
        }
    }
}