using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.WebApp.Application.Chain.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Chain
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(CoreApplicationWebAppAElfModule))]
    public class ChainApplicationWebAppAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBestChainFoundLogEventHandler, MiningInformationUpdatedLogEventHandler>();
        }
    }
}