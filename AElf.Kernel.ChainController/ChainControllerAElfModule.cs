using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(typeof(SmartContractExecutionAElfModule))]
    public class ChainControllerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddAssemblyOf<ChainControllerAElfModule>();
        }
    }
}