using AElf.Contracts.Genesis;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.ChainController;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Runtime.CSharp;
using AElf.Runtime.CSharp.ExecutiveTokenPlugin;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Contracts.TestKit
{
    #region Dummy
    [DependsOn(
        typeof(AbpTestBaseModule))]
    public class TestBaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            ITestOutputHelperAccessor testOutputHelperAccessor = new TestOutputHelperAccessor();

            context.Services.AddSingleton(testOutputHelperAccessor);

            context.Services.AddLogging(o =>
            {
                o.AddXUnit(testOutputHelperAccessor);
                 
            });
        }
    }

    public class TestOutputHelperAccessor : ITestOutputHelperAccessor
    {
        public ITestOutputHelper OutputHelper { get; set; }
    }

    [DependsOn(
        typeof(TestBaseAElfModule), typeof(CoreKernelAElfModule))]
    public class TestBaseKernelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());

//            services.AddSingleton<KernelTestHelper>();
        }
    }
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(TestBaseKernelAElfModule))]
    public class KernelCoreTestAElfModule : AElfModule
    {
    }
    [DependsOn(
        typeof(SmartContractAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class SmartContractTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<SmartContractRunnerContainer>();
        }
    }
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
    [DependsOn(
        typeof(TransactionPoolAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class TransactionPoolTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<TxHub>();
        }
    }
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            services.AddTransient<ChainCreationService>();
        }
    }
    #endregion
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule),
        typeof(KernelAElfModule),
        typeof(ConsensusAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(SmartContractTestAElfModule),
        typeof(SmartContractExecutionTestAElfModule),
        typeof(TransactionPoolTestAElfModule),
        typeof(ChainControllerTestAElfModule)
    )]
    public class ContractTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            services.AddSingleton(o => Mock.Of<IPeerPool>());

            services.AddSingleton(o => Mock.Of<INetworkService>());
            
            // When testing contract and packaging transactions, no need to generate and schedule real consensus stuff.
            context.Services.AddSingleton(o => Mock.Of<IConsensusInformationGenerationService>());
            context.Services.AddSingleton(o => Mock.Of<IConsensusScheduler>());
            context.Services.AddTransient(o => Mock.Of<IConsensusService>());
            context.Services.AddTransient(o => Mock.Of<IAccountService>());
            context.Services.AddTransient<IContractTesterFactory, ContractTesterFactory>();
            context.Services.AddTransient<ITransactionExecutor, TransactionExecutor>();
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        }

        public int ChainId { get; } = 500;
        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = ChainId,
                ZeroSmartContract = typeof(BasicContractZero),
                SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
            };
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
}