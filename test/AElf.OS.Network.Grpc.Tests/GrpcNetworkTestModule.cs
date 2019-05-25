using System;
using AElf.Kernel;
using AElf.Kernel.Node.Application;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            });
            
            context.Services.AddTransient<IBlockChainNodeStateService>(o =>
            {
                var mockService = new Mock<IBlockChainNodeStateService>();
                mockService.Setup(a => a.IsNodeSyncing()).Returns(false);
                return mockService.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);
            
            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var channel = new Channel(GrpcTestConstants.FakeListeningPort, ChannelCredentials.Insecure);
            pool.AddPeer(new GrpcPeer(channel, new PeerService.PeerServiceClient(channel), GrpcTestConstants.FakePubKey2,
                GrpcTestConstants.FakeListeningPort, KernelConstants.ProtocolVersion,
                DateTime.UtcNow.ToTimestamp().Seconds, 1));

        }
    }
}