using AElf.ContractTestKit;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests;

[DependsOn(typeof(ContractTestModule),
    typeof(ExecutionPluginForResourceFeeModule),
    typeof(FeeCalculationModule))]
public class ExecutionPluginForResourceFeeTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.AddTransient(typeof(ILogEventProcessingService<>), typeof(LogEventProcessingService<>));
        context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
    }
}