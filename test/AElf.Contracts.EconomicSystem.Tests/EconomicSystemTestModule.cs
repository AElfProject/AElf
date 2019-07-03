using AElf.Contracts.Economic.TestBase;
using Volo.Abp.Modularity;

namespace AElf.Contracts.EconomicSystem.Tests
{
    [DependsOn(typeof(EconomicContractsTestModule))]
    public class EconomicSystemTestModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}