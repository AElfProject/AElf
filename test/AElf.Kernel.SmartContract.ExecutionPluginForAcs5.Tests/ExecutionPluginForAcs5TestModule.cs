using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using TokenContractChargeFeeStrategy = AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests.TokenContractChargeFeeStrategy;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs5Module))]
    public class ExecutionPluginForAcs5TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<IPreExecutionPlugin, MethodCallingThresholdPreExecutionPlugin>();
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
        }
    }
}