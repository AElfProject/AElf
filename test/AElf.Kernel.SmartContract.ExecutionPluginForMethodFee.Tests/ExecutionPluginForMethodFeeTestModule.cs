using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.TestBase;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.Service;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

[DependsOn(
    typeof(ContractTestModule),
    typeof(ExecutionPluginForMethodFeeModule),
    typeof(FeeCalculationModule))]
public class ExecutionPluginForMethodFeeTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
        context.Services.AddTransient(typeof(ILogEventProcessingService<>), typeof(LogEventProcessingService<>));
        context.Services.RemoveAll(s => s.ImplementationType == typeof(TransactionFeeChargedLogEventProcessor));
        context.Services.AddTransient<IBlockAcceptedLogEventProcessor, TransactionFeeChargedLogEventProcessor>();
        context.Services.RemoveAll<IContractInitializationProvider>();
        context.Services
            .AddTransient<IContractInitializationProvider, MethodFeeTestTokenContractInitializationProvider>();
        context.Services
            .AddTransient<IContractInitializationProvider, AEDPoSContractInitializationProvider>();
        context.Services
            .AddTransient<IAEDPoSContractInitializationDataProvider, AEDPoSContractInitializationDataProvider>();
        context.Services
            .AddTransient<IGenesisSmartContractDtoProvider, MethodFeeTestGenesisSmartContractDtoProvider>();
    }
}

[DependsOn(
    typeof(ContractTestModule),
    typeof(ExecutionPluginForMethodFeeModule),
    typeof(FeeCalculationModule))]
public class ExecutionPluginForUserContractMethodFeeTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
        context.Services.RemoveAll<IContractInitializationProvider>();
        context.Services
            .AddTransient<IContractInitializationProvider, MethodFeeTestTokenContractInitializationProvider>();
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
        context.Services.AddSingleton<ICalculateFunctionProvider, MockCalculateFunctionProvider>();
        context.Services.AddSingleton<ISystemTransactionGenerator, MockTransactionGenerator>();
        context.Services.RemoveAll<IAccountService>();
        var initializeMiner = SampleAccount.Accounts[0].KeyPair;
        context.Services.AddTransient(o =>
        {
            var mockService = new Mock<IAccountService>();
            mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                Task.FromResult(CryptoHelper.SignWithPrivateKey(initializeMiner.PrivateKey, data)));

            mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(initializeMiner.PublicKey);

            return mockService.Object;
        });
        context.Services
            .AddTransient<CleanBlockExecutedDataChangeHeightEventHandler>();
    }
}

[DependsOn(
    typeof(ContractTestModule))]
public class ExecutionPluginTransactionDirectlyForMethodFeeTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        context.Services.RemoveAll<IContractInitializationProvider>();
        context.Services.RemoveAll<IPreExecutionPlugin>();
        context.Services.RemoveAll<IPostExecutionPlugin>();
        context.Services.Replace(ServiceDescriptor
            .Singleton<IPlainTransactionExecutingService, PlainTransactionExecutingAsPluginService>());
        context.Services.Replace(ServiceDescriptor
            .Singleton<ITransactionExecutingService, PlainTransactionExecutingAsPluginService>());
    }
}