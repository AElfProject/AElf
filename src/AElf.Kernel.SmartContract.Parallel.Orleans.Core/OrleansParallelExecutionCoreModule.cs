using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Parallel.Orleans
{
    [DependsOn(typeof(ParallelExecutionModule))]
    public class OrleansParallelExecutionCoreModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ClusterOptions>(configuration.GetSection("Orleans:Cluster"));
        }
    }
}