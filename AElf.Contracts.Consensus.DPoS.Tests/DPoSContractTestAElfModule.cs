using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    [DependsOn(
        typeof(TestBase.ContractTestAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class DPoSContractTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<DPoSContractTestAElfModule>();
        }
    }
}