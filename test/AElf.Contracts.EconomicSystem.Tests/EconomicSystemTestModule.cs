using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.EconomicSystem
{
    [DependsOn(typeof(ContractTestModule))]
    public class EconomicSystemTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<EconomicSystemTestModule>();
        }
    }
}