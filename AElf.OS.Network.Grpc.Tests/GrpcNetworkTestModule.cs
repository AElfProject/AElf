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
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);

            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            pool.AddPeer(new GrpcPeer(new Channel(GrpcTestConstants.FakeListeningPort, ChannelCredentials.Insecure),
                null, GrpcTestConstants.FakePubKey, GrpcTestConstants.FakeListeningPort));
        }
    }
}