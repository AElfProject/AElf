using AElf.Contracts.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Resource
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class ResourceContractTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ResourceContractTestAElfModule>();
        }
    }
}