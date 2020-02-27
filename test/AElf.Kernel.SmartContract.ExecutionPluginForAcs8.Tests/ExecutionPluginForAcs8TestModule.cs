using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs8Module))]
    public class ExecutionPluginForAcs8TestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICalculateTxCostStrategy, TestCalculateTxStrategy>();
            context.Services.AddSingleton<ICalculateReadCostStrategy, TestCalculateReadStrategy>();
            context.Services.AddSingleton<ICalculateStorageCostStrategy, TestCalculateStorageStrategy>();
            context.Services.AddSingleton<ICalculateWriteCostStrategy, TestCalculateWriteStrategy>();
            context.Services.AddSingleton<ICalculateTrafficCostStrategy, TestCalculateTrafficStrategy>();
        }
    }
}