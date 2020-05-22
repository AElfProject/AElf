using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Deployer;
using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.ContractTestBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractInitialization;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    [DependsOn(
        typeof(MainChainContractTestModule),
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForMethodFeeTestModule : MainChainContractTestModule
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
            context.Services.AddSingleton(o =>
            {
                var mockService = new Mock<IConsensusService>();
                mockService.Setup(s =>
                        s.GenerateConsensusTransactionsAsync(It.IsAny<ChainContext>()))
                    .Returns(Task.FromResult(new List<Transaction>()));
                return mockService.Object;
            });
            
            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockValidationService>();
                mockService.Setup(s =>
                        s.ValidateBlockBeforeExecuteAsync(It.IsAny<IBlock>()))
                    .Returns(Task.FromResult(true));
                mockService.Setup(s =>
                        s.ValidateBlockAfterExecuteAsync(It.IsAny<IBlock>()))
                    .Returns(Task.FromResult(true));
                return mockService.Object;
            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<ExecutionPluginForMethodFeeTestModule>();
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