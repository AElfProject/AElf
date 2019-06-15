using AElf.Contracts.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.ParliamentAuth
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class ParliamentAuthContractTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ParliamentAuthContractTestAElfModule>();
        }
    }
}