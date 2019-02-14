using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IAElfNetworkServer, GrpcNetworkServer>();
            context.Services.AddSingleton<INetworkService, GrpcNetworkService>();
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var myService = context.ServiceProvider.GetService<IAElfNetworkServer>();
            myService.StartAsync();
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var myService = context.ServiceProvider.GetService<IAElfNetworkServer>();
            myService.StartAsync();
        }
    }
}