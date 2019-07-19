using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Vote
{
    [DependsOn(typeof(ContractTestModule))]
    public class VoteContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}