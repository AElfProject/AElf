using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class SmartContractAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<SmartContractAElfModule>();

            context.Services.AddSingleton<ISmartContractRunnerContainer, SmartContractRunnerContainer>();

            context.Services.AddSingleton<IDefaultContractZeroCodeProvider, DefaultContractZeroCodeProvider>();
        }
    }
}