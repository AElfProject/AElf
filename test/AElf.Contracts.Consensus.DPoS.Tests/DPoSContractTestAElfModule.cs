using AElf.Contracts.TestBase;
using AElf.Kernel.Consensus.AElfConsensus.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    [DependsOn(typeof(ContractTestAElfModule))]
    public class DPoSContractTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<DPoSContractTestAElfModule>();
            context.Services.AddSingleton<IConsensusInformationGenerationService, AElfConsensusInformationGenerationService>();
        }
    }
}