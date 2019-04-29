using AElf.Contracts.TestBase;
using AElf.Kernel.Consensus.AElfConsensus.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.DPoS.SideChain
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class DPoSSideChainTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<DPoSSideChainTestAElfModule>();
            context.Services.AddSingleton<IConsensusInformationGenerationService, AElfConsensusInformationGenerationService>();
        }
    }
}