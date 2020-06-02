using System.Collections.Generic;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
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
    public class SideChainContractTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string> {SampleAccount.Accounts[0].KeyPair.PublicKey.ToHex()};
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
    }
}