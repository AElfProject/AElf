using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests;
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
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICalculateTxCostStrategy, TestCalculateTxStrategy>();
            context.Services.AddSingleton<ICalculateCpuCostStrategy, TestCalculateCpuStrategy>();
            context.Services.AddSingleton<ICalculateStoCostStrategy, TestCalculateStoStrategy>();
            context.Services.AddSingleton<ICalculateRamCostStrategy, TestCalculateRamStrategy>();
            context.Services.AddSingleton<ICalculateNetCostStrategy, TestCalculateNetStrategy>();
        }
    }
}