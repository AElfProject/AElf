using System.Threading.Tasks;
using Acs0;
using AElf.Cryptography;
using AElf.Contracts.Genesis;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.ChainController;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Runtime.CSharp;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Contracts.TestKit
{
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
        typeof(CSharpRuntimeAElfModule)
    )]
    public class ContractTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
            });

            #region Infra

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddTransient<ChainCreationService>();
            services.AddSingleton<TxHub>();
            services.AddSingleton<SmartContractRunnerContainer>();

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

            // When testing contract and packaging transactions, no need to generate and schedule real consensus stuff.
//            context.Services.AddSingleton(o => Mock.Of<IConsensusInformationGenerationService>());
//            context.Services.AddSingleton(o => Mock.Of<IConsensusScheduler>());
            context.Services.AddTransient(o => Mock.Of<IConsensusService>());
            #endregion

            context.Services.AddTransient<IAccount, Account>();
            context.Services.AddTransient<IContractTesterFactory, ContractTesterFactory>();
            context.Services.AddTransient<ITransactionExecutor, TransactionExecutor>();
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        }

        public int ChainId { get; } = 500;
        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            context.ServiceProvider.GetService<IAElfAsymmetricCipherKeyPairProvider>().SetKeyPair(CryptoHelper.GenerateKeyPair());

            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = ChainId,
                ZeroSmartContract = typeof(BasicContractZero),
                SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory,
            };            
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

    public class TestOutputHelperAccessor : ITestOutputHelperAccessor
    {
        public ITestOutputHelper OutputHelper { get; set; }
    }
}