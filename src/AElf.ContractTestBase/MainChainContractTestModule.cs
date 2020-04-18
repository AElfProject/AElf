using System.Collections.Generic;
using AElf.Blockchains.ContractInitialization;
using AElf.Contracts.TestKit;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ContractTestBase
{
    [DependsOn(
        typeof(ContractTestModule),
        typeof(MainChainContractInitializationAElfModule)
    )]
    public class MainChainContractTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList = new List<string> {SampleECKeyPairs.KeyPairs[0].PublicKey.ToHex()};
            });

            context.Services.AddTransient<IGenesisSmartContractDtoProvider, GenesisSmartContractDtoProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }
    }
}