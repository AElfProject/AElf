using AElf.Modularity;
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
            context.Services.AddSingleton<IPeerPool, GrpcPeerPool>();
            context.Services.AddSingleton<INetworkService, GrpcNetworkService>();
            
            context.Services.AddSingleton<PeerService.PeerServiceBase, GrpcServerService>();
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var myService = context.ServiceProvider.GetService<IAElfNetworkServer>();
            AsyncHelper.RunSync(myService.StartAsync);
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var myService = context.ServiceProvider.GetService<IAElfNetworkServer>();
            AsyncHelper.RunSync(myService.StopAsync);
        }
    }
}