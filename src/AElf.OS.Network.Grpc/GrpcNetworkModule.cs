using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer, GrpcNetworkServer>();
            context.Services.AddSingleton<GrpcPeerPool>();
            context.Services.AddSingleton<IPeerPool, GrpcPeerPool>();

            context.Services.AddSingleton<PeerService.PeerServiceBase, GrpcServerService>();
            
            context.Services.AddSingleton<AuthInterceptor>();
        }
    }
}