using System.Collections.Generic;
using AElf.ContractTestKit;
using AElf.CSharp.CodeOps;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contract.TestContract;

[DependsOn(typeof(CSharpCodeOpsAElfModule),typeof(ContractTestModule))]
public class TestContractAElfModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.RemoveAll<IBlockValidationProvider>();
    }
}

[DependsOn(
    typeof(ContractTestModule),
    typeof(ExecutionPluginForMethodFeeModule),
    typeof(ExecutionPluginForResourceFeeModule),
    typeof(FeeCalculationModule))]
public class TestFeesContractAElfModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
        context.Services.AddTransient(typeof(ILogEventProcessingService<>), typeof(LogEventProcessingService<>));
        //TODO Fix never claim transaction fee
        context.Services.RemoveAll(s =>
            s.ImplementationType != null && s.ImplementationType.FullName != null &&
            s.ImplementationType.FullName.Contains("TransactionFeeChargedLogEventProcessor"));
    }
}