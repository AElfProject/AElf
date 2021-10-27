using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestBase.Tests
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class TestBaseTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}