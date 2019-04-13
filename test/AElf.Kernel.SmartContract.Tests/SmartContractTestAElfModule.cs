using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(
        typeof(SmartContractAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class SmartContractTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<SmartContractRunnerContainer>();
        }
    }
}