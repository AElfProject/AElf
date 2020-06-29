using AElf.ContractTestKit;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TokenHolder
{
    [DependsOn(typeof(ContractTestModule))]
    public class TokenHolderContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.RemoveAll<IPostExecutionPlugin>();
            context.Services.RemoveAll<ISystemTransactionGenerator>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProvider>();
        }
    }
}