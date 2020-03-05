using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using TokenContractChargeFeeStrategy = AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.TokenContractChargeFeeStrategy;

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
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICoefficientsCacheProvider, MockCoefficientProvider>();
        }
    }
}