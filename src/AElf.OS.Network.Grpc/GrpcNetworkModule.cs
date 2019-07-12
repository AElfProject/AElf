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

            
            context.Services.AddSingleton<GrpcPeerPool, GrpcPeerPool>();
            context.Services.AddSingleton<IPeerPool>(p => p.GetService<GrpcPeerPool>());
            
            context.Services.AddSingleton<IPeerDialer, PeerDialer>();

            context.Services.AddSingleton<PeerService.PeerServiceBase, GrpcServerService>();
            
            context.Services.AddSingleton<AuthInterceptor>();
            context.Services.AddSingleton<RetryInterceptor>();
        }
    }
}