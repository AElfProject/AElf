using System.Collections.Generic;
using AElf.ContractTestKit;
using AElf.CrossChain;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ContractTestBase
{
    [DependsOn(
        typeof(ContractTestModule),
        typeof(AEDPoSAElfModule),
        typeof(TokenKernelAElfModule),
        typeof(CrossChainCoreModule),
        typeof(EconomicSystemAElfModule),
        typeof(GovernmentSystemAElfModule)
    )]
    public class MainChainContractTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AEDPoSOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string> {SampleAccount.Accounts[0].KeyPair.PublicKey.ToHex()};
            });

            var services = context.Services;
            services.AddTransient<IGenesisSmartContractDtoProvider, GenesisSmartContractDtoProvider>();
            services.AddTransient<IContractDeploymentListProvider, MainChainContractDeploymentListProvider>();
            services.AddTransient<IParliamentContractInitializationDataProvider,
                MainChainParliamentContractInitializationDataProvider>();
            services.AddTransient<IAEDPoSContractInitializationDataProvider,
                MainChainAEDPoSContractInitializationDataProvider>();
            services.AddTransient<ITokenContractInitializationDataProvider,
                MainChainTokenContractInitializationDataProvider>();
            services.AddTransient<ICrossChainContractInitializationDataProvider,
                MainChainCrossChainContractInitializationDataProvider>();
        }

        // TODO: Our test module needs to inherit TestKit's ContractTestModule.
        // If it is not overwritten with an empty method,
        // it will inherit TestKit's ContractTestModule logic and execute it many times.
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }
    }
}