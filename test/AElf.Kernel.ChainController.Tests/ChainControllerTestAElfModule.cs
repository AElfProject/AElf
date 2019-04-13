using AElf.Kernel.ChainController.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            services.AddTransient<ChainCreationService>();
        }
    }
}