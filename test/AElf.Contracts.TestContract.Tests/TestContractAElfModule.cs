using System.Collections.Generic;
using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
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
            context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
            context.Services.AddTransient(typeof(ILogEventListeningService<>), typeof(LogEventListeningService<>));
            //TODO Fix never claim transaction fee
            context.Services.RemoveAll(s => s.ImplementationType == typeof(TransactionFeeChargedLogEventProcessor));
        } 
    }
}