using AElf.Contracts.TestBase;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.Tests
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class ConsensusContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ConsensusContractTestAElfModule>();
        }
    }
}