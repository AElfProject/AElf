using AElf.Contracts.TestBase;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Token
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class TokenContractTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //var type = typeof(TokenContract).Assembly;
        }
    }
}