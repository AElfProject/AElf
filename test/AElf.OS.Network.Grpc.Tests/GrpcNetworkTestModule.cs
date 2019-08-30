using System;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(GrpcNetworkModule))]
    public class GrpcNetworkTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o=>
            {
                o.ListeningPort = 2000;
                o.MaxPeers = 2;
            });
            
            context.Services.AddSingleton(o =>
            {
                var mockService = new Mock<ISyncStateService>();
                mockService.Setup(s => s.SyncState).Returns(SyncState.Finished);
                return mockService.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);
            
            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var channel = new Channel(NetworkTestConstants.FakeIpEndpoint, ChannelCredentials.Insecure);
            
            var connectionInfo = new PeerInfo
            {
                Pubkey = NetworkTestConstants.FakePubkey2,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                IsInbound = true
            };
            
            if (!IpEndpointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out var peerEnpdoint))
                throw new Exception($"Ip {NetworkTestConstants.FakeIpEndpoint} is invalid.");
            
            pool.TryAddPeer(new GrpcPeer(new GrpcClient(channel, new PeerService.PeerServiceClient(channel)), peerEnpdoint, connectionInfo));
        }
    }

    [DependsOn(
        typeof(GrpcNetworkTestModule))]
    public class GrpcNetworkDialerTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConnectionService>();
                mockService.Setup(m =>m.ConnectAsync(It.IsAny<IPEndPoint>()))
                    .Returns(Task.FromResult(true));
                mockService.Setup(m=>m.DialBackAsync(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionInfo>()))
                    .Returns(Task.FromResult(new ConnectReply
                    {
                        Error = ConnectError.ConnectOk,
                        Info = new ConnectionInfo
                        {
                            ChainId = 1,
                            ListeningPort = 2000,
                            Pubkey = ByteString.CopyFromUtf8("pubkey"),
                            Version = 1
                        }
                    }));

                return mockService.Object;
            });
        }
    }
}