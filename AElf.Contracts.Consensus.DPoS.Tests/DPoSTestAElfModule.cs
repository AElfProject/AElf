using AElf.Contracts.TestKit;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.DPoS
{
    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class DPoSTestAElfModule : ContractTestModule
    {
    }
}