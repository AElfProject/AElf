using System.Collections.Generic;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
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
    public class MainChainContractTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string> {SampleAccount.Accounts[0].KeyPair.PublicKey.ToHex()};
            });

            var services = context.Services;
            services.AddTransient<IContractDeploymentListProvider, MainChainContractDeploymentListProvider>();
            services.AddTransient<IParliamentContractInitializationDataProvider,
                MainChainParliamentContractInitializationDataProvider>();
            services.AddTransient<IAEDPoSContractInitializationDataProvider,
                AEDPoSContractInitializationDataProvider>();
            services.AddTransient<ITokenContractInitializationDataProvider,
                MainChainTokenContractInitializationDataProvider>();
            services.AddTransient<ICrossChainContractInitializationDataProvider,
                MainChainCrossChainContractInitializationDataProvider>();
        }
    }
}