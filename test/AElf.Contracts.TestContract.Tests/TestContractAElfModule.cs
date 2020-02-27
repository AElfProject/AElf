using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contract.TestContract
{
    [DependsOn(typeof(ContractTestModule))]
    public class TestContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
        }
    }

    [DependsOn(
        typeof(ContractTestModule),
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(ExecutionPluginForAcs8Module))]
    public class TestFeesContractAElfModule : ContractTestModule
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