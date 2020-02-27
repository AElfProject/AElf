using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForResourceFeeModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, DonateResourceTransactionGenerator>();
        }
    }
}