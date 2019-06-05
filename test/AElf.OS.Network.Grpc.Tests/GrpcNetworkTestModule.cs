using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreWithChainTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcNetworkTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o=>
            {
                o.ListeningPort = 2000;
                o.MaxPeers = 2;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);
            
            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var channel = new Channel(GrpcTestConstants.FakeListeningPort, ChannelCredentials.Insecure);
            
            var connectionInfo = new GrpcPeerInfo
            {
                PublicKey = GrpcTestConstants.FakePubKey2,
                PeerIpAddress = GrpcTestConstants.FakeListeningPort,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = 1,
                IsInbound = true
            };
            
            pool.AddPeer(new GrpcPeer(channel, new PeerService.PeerServiceClient(channel), connectionInfo));
        }
    }
}