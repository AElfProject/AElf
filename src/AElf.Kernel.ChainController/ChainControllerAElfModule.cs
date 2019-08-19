using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(typeof(SmartContractExecutionAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class ChainControllerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}