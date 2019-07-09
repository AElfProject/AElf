using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class BasicContractZeroTestAElfModule : ContractTestAElfModule
    {
    }

    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class AuthorityNotRequiredBasicContractZeroTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}