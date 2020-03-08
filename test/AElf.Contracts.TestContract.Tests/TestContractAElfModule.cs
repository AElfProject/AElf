using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
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
        typeof(ExecutionPluginForResourceFeeModule),
        typeof(FeeCalculationModule))]
    public class TestFeesContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICoefficientsProvider, MockFeeCalculateCoefficientProvider>();
        } 
    }
}