using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer, GrpcNetworkServer>();
            context.Services.AddSingleton<GrpcPeerPool>();
            context.Services.AddSingleton<INetworkService, NetworkService>();
            context.Services.AddSingleton<IPeerPool, GrpcPeerPool>();

            context.Services.AddSingleton<PeerService.PeerServiceBase, GrpcServerService>();
        }
    }
}