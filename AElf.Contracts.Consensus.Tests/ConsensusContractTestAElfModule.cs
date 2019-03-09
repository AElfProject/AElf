using AElf.Contracts.TestBase;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus
{
    [DependsOn(typeof(ContractTestAElfModule))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ConsensusContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ConsensusContractTestAElfModule>();
        }
    }
}