using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForResourceFeeModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForResourceFeeTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICoefficientsProvider, MockFeeCalculateCoefficientProvider>();
        }
    }
}