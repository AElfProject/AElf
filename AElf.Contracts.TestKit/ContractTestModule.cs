using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Runtime.CSharp;
using AElf.Runtime.CSharp.ExecutiveTokenPlugin;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestKit
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule),
        typeof(KernelTestAElfModule)
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