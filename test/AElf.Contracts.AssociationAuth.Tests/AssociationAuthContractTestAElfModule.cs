using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.AssociationAuth
{
    [DependsOn(typeof(ContractTestModule))]
    public class AssociationAuthContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AssociationAuthContractTestAElfModule>();
        }
    }
}