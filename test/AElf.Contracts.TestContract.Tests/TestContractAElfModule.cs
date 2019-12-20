using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
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
        typeof(ExecutionPluginForAcs1Module),
        typeof(ExecutionPluginForAcs8Module))]
    public class TestFeesContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICalculateTxCostStrategy, TestCalculateTxStrategy>();
            context.Services.AddSingleton<ICalculateCpuCostStrategy, TestCalculateCpuStrategy>();
            context.Services.AddSingleton<ICalculateStoCostStrategy, TestCalculateStoStrategy>();
            context.Services.AddSingleton<ICalculateRamCostStrategy, TestCalculateRamStrategy>();
            context.Services.AddSingleton<ICalculateNetCostStrategy, TestCalculateNetStrategy>();
        } 
    }
}