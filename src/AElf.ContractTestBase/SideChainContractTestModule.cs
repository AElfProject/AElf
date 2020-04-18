using System.Collections.Generic;
using AElf.Blockchains.ContractInitialization;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ContractTestBase
{
    [DependsOn(
        typeof(ContractTestModule),
        typeof(SideChainContractInitializationAElfModule)
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

            context.Services.AddTransient<IGenesisSmartContractDtoProvider, GenesisSmartContractDtoProvider>();
            context.Services.AddTransient<ISideChainInitializationDataProvider, SideChainInitializationDataProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }
    }
}