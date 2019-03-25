using AElf.Contracts.TestBase2;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    [DependsOn(
        typeof(ContractTestAElfModule2)
    )]
    public class BasicContractZeroTestAElfModule : ContractTestAElfModule2
    {
    }
}