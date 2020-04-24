using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForCallThresholdModule))]
    public class ExecutionPluginForCallThresholdTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IPreExecutionPlugin, MethodCallingThresholdPreExecutionPlugin>();
            // context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
        }
    }
}