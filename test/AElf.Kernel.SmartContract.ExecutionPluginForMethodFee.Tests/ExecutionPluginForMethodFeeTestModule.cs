using System.Collections.Generic;
using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
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
            context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
            context.Services.AddSingleton<MethodFeeAffordableValidationProvider>();
            context.Services.AddSingleton<TransactionMethodNameValidationProvider>();
            context.Services.AddSingleton<TxHubEntryPermissionValidationProvider>();
            context.Services.AddTransient(typeof(ILogEventListeningService<>), typeof(LogEventListeningService<>));
            context.Services.RemoveAll(s=>s.ImplementationType == typeof(TransactionFeeChargedLogEventProcessor));
            context.Services.AddTransient<IBlockAcceptedLogEventProcessor,TransactionFeeChargedLogEventProcessor>();
        }
    }
    
    [DependsOn(typeof(ContractTestAElfModule),
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForMethodFeeWithForkTestModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddSingleton<ITransactionFeeExemptionService, TransactionFeeExemptionService>();
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
        }
    }
}