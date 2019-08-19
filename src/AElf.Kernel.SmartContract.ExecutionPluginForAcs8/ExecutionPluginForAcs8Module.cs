using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8
{
    [DependsOn(typeof(SmartContractAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class ExecutionPluginForAcs8Module : AElfModule<ExecutionPluginForAcs8Module>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, DonateResourceTransactionGenerator>();
        }
    }
}