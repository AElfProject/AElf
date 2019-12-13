using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using TokenContractChargeFeeStrategy = AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests.TokenContractChargeFeeStrategy;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs8Module))]
    public class ExecutionPluginForAcs8TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o =>
            {
                o.ContractDeploymentAuthorityRequired = false;
                o.TransactionExecutionCounterThreshold = -1;
            });
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
        }
    }
}