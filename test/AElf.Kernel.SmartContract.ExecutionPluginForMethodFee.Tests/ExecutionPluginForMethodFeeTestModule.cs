using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForMethodFeeTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddSingleton<ITransactionFeeExemptionService, TransactionFeeExemptionService>();
            context.Services.AddSingleton<IChargeFeeStrategy, TestContractChargeFeeStrategy>();
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICoefficientsProvider, MockFeeCalculateCoefficientProvider>();
        }
    }
}