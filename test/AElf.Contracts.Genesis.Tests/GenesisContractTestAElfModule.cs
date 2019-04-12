using AElf.Contracts.TestKit;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class BasicContractZeroTestAElfModule : ContractTestModule
    {
    }
}