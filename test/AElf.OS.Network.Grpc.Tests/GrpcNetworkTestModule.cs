using System;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using QuickGraph;
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
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var syncStateProvider = context.ServiceProvider.GetRequiredService<INodeSyncStateProvider>();
            syncStateProvider.SetSyncTarget(-1);
            
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