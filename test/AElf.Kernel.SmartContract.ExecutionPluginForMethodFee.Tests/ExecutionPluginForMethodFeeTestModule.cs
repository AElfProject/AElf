using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Deployer;
using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractInitialization;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    [DependsOn(
        typeof(ContractTestModule),
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForMethodFeeTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
            context.Services.AddTransient(typeof(ILogEventProcessingService<>), typeof(LogEventProcessingService<>));
            context.Services.RemoveAll(s => s.ImplementationType == typeof(TransactionFeeChargedLogEventProcessor));
            context.Services.AddTransient<IBlockAcceptedLogEventProcessor, TransactionFeeChargedLogEventProcessor>();
            context.Services.RemoveAll<IContractInitializationProvider>();
            context.Services
                .AddTransient<IContractInitializationProvider, MethodFeeTestTokenContractInitializationProvider>();
            context.Services
                .AddTransient<IGenesisSmartContractDtoProvider, MethodFeeTestGenesisSmartContractDtoProvider>();
        }
    }

    [DependsOn(typeof(ContractTestAElfModule),
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForMethodFeeWithForkTestModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
            context.Services.AddSingleton<ISystemTransactionGenerator,MockTransactionGenerator>();
        }
    }
}