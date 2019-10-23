using System;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Protocol.Types;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
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
            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var channel = new Channel(NetworkTestConstants.FakeIpEndpoint, ChannelCredentials.Insecure);
            
            var connectionInfo = new PeerConnectionInfo
            {
                Pubkey = NetworkTestConstants.FakePubkey2,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow(),
                IsInbound = true
            };
            
            if (!IpEndPointHelper.TryParse(NetworkTestConstants.FakeIpEndpoint, out var peerEndpoint))
                throw new Exception($"Ip {NetworkTestConstants.FakeIpEndpoint} is invalid.");
            
            pool.TryAddPeer(new GrpcPeer(new GrpcClient(channel, new PeerService.PeerServiceClient(channel)), peerEndpoint, connectionInfo));
        }
    }
}