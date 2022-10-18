using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.ChainController;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Runtime.CSharp;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.ContractTestBase.ContractTestKit;

public class ContractTestModule<TSelf> : ContractTestModule
    where TSelf : ContractTestModule<TSelf>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAssemblyOf<TSelf>();
        base.ConfigureServices(context);
    }
}

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(AbpTestBaseModule),
    typeof(CoreKernelAElfModule),
    typeof(KernelAElfModule),
    typeof(NodeAElfModule),
    typeof(CoreOSAElfModule),
    typeof(SmartContractAElfModule),
    typeof(SmartContractExecutionAElfModule),
    typeof(TransactionPoolAElfModule),
    typeof(ChainControllerAElfModule),
    typeof(CSharpRuntimeAElfModule))]
public class ContractTestModule : AbpModule
{
    public int ChainId { get; } = ChainHelper.ConvertBase58ToChainId("AELF");
    public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        Configure<HostSmartContractBridgeContextOptions>(options =>
        {
            options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
            options.ContextVariables["SymbolListToPayTxFee"] = "WRITE,READ,STORAGE,TRAFFIC";
            options.ContextVariables["SymbolListToPayRental"] = "CPU,RAM,DISK,NET";
        });

        Configure<ChainOptions>(options =>
        {
            if (options.ChainId == 0) options.ChainId = ChainId;
        });

        #region Infra

        services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
        services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        // services.AddTransient<ChainCreationService>();
        // services.AddSingleton<TxHub>();
        // services.AddSingleton<SmartContractRunnerContainer>();

        #endregion

        #region Logger

        ITestOutputHelperAccessor testOutputHelperAccessor = new TestOutputHelperAccessor();
        services.AddSingleton(testOutputHelperAccessor);
        services.AddLogging(o => { o.AddXUnit(testOutputHelperAccessor); });

        #endregion

        #region Mocks

        services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
        services.AddSingleton(o => Mock.Of<IPeerPool>());

        services.AddSingleton(o => Mock.Of<INetworkService>());

        context.Services.AddTransient(o => Mock.Of<IConsensusService>());

        #endregion

        context.Services.AddTransient<IContractTesterFactory, ContractTesterFactory>();
        context.Services.AddTransient<ITestTransactionExecutor, TestTransactionExecutor>();
        context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        context.Services.Replace(ServiceDescriptor
            .Singleton<ITransactionExecutingService, PlainTransactionExecutingService>());
        context.Services.AddSingleton<ISmartContractRunner, UnitTestCSharpSmartContractRunner>(provider =>
        {
            var option = provider.GetService<IOptions<RunnerOptions>>();
            return new UnitTestCSharpSmartContractRunner(
                option.Value.SdkDir);
        });
        context.Services.AddSingleton<IDefaultContractZeroCodeProvider, UnitTestContractZeroCodeProvider>();
        context.Services.AddSingleton<ISmartContractAddressService, UnitTestSmartContractAddressService>();
        context.Services.RemoveAll(s => s.ImplementationType == typeof(ConsensusRequestMiningEventHandler));
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.ServiceProvider.GetService<IAElfAsymmetricCipherKeyPairProvider>()
            .SetKeyPair(SampleAccount.Accounts[0].KeyPair);

        var dto = new OsBlockchainNodeContextStartDto
        {
            ChainId = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value.ChainId,
            ZeroSmartContract = typeof(BasicContractZero),
            SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
        };
        var dtoProvider = context.ServiceProvider.GetRequiredService<IGenesisSmartContractDtoProvider>();
        dto.InitializationSmartContracts = dtoProvider.GetGenesisSmartContractDtos().ToList();
        var contractOptions = context.ServiceProvider.GetService<IOptionsSnapshot<ContractOptions>>().Value;
        dto.ContractDeploymentAuthorityRequired = contractOptions.ContractDeploymentAuthorityRequired;
        var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
        var that = this;
        AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
        var that = this;
        AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
    }
}

[DependsOn(typeof(ContractTestModule),
    typeof(ExecutionPluginForResourceFeeModule),
    typeof(ExecutionPluginForCallThresholdModule),
    typeof(ExecutionPluginForMethodFeeModule))]
public class ContractTestWithExecutionPluginModule : AbpModule
{
}

public class TestOutputHelperAccessor : ITestOutputHelperAccessor
{
    public ITestOutputHelper OutputHelper { get; set; }
}