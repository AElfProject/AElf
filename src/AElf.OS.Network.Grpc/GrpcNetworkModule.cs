using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkModule : AElfModule
    {
        /// <summary>
        /// Registers the components implemented by the gRPC library.
        /// </summary>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer, GrpcNetworkServer>();
            context.Services.AddTransient<IPeerDialer, PeerDialer>();

            context.Services.AddSingleton<PeerService.PeerServiceBase, GrpcServerService>();
            
            context.Services.AddSingleton<AuthInterceptor>();
            context.Services.AddSingleton<RetryInterceptor>();
        }
    }
}