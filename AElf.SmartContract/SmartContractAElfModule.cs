using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.SmartContract
{
    [DependsOn(typeof(KernelAElfModule))]
    public class SmartContractAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<SmartContractAElfModule>();

            context.Services.AddSingleton<ISmartContractRunnerContainer, SmartContractRunnerContainer>();

        }
    }
}