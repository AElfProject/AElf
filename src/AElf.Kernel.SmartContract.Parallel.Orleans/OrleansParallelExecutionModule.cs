using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Orleans.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Parallel.Orleans
{
    [DependsOn(typeof(OrleansParallelExecutionCoreModule))]
    public class OrleansParallelExecutionModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ClusterOptions>(configuration.GetSection("Orleans:Cluster"));
            Configure<StaticGatewayListProviderOptions>(configuration.GetSection("Orleans:GatewayList"));
            
            context.Services.AddSingleton(_ => _.GetService<IClusterClientService>().Client);
            context.Services.AddSingleton<ITransactionExecutingService, OrleansParallelTransactionExecutingService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var clientService = context.ServiceProvider.GetService<IClusterClientService>();
            AsyncHelper.RunSync(() => clientService.StartAsync());
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var clientService = context.ServiceProvider.GetService<IClusterClientService>();
            AsyncHelper.RunSync(() => clientService.StopAsync());
        }
    }
}