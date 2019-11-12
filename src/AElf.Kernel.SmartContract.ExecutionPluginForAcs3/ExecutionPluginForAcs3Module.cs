using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs3
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForAcs3Module : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, ProposalApprovalTransactionGenerator>();
        }
    }
}