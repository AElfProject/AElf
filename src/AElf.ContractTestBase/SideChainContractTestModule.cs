using System.Collections.Generic;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContractInitialization;
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
    public class SideChainContractTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string> {SampleECKeyPairs.KeyPairs[0].PublicKey.ToHex()};
            });
            
            Configure<CrossChainConfigOptions>(options => { options.ParentChainId = "AELF"; });

            var services = context.Services;
            services.AddTransient<IGenesisSmartContractDtoProvider, GenesisSmartContractDtoProvider>();
            services.AddTransient<ISideChainInitializationDataProvider, SideChainInitializationDataProvider>();
            services.AddTransient<IContractDeploymentListProvider, SideChainContractDeploymentListProvider>();
            services.AddTransient<IParliamentContractInitializationDataProvider,
                SideChainParliamentContractInitializationDataProvider>();
            services.AddTransient<IAEDPoSContractInitializationDataProvider,
                SideChainAEDPoSContractInitializationDataProvider>();
            services.AddTransient<ITokenContractInitializationDataProvider,
                SideChainTokenContractInitializationDataProvider>();
            services.AddTransient<ICrossChainContractInitializationDataProvider,
                SideChainCrossChainContractInitializationDataProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }
    }
}