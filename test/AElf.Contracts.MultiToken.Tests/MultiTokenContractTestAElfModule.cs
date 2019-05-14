using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.MultiToken
{
    [DependsOn(typeof(ContractTestModule))]
    public class MultiTokenContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<MultiTokenContractTestAElfModule>();
        }
    }
}