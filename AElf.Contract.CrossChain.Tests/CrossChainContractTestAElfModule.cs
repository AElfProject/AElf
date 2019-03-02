using AElf.Contracts.TestBase;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Contract.CrossChain.Tests
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class CrossChainContractTestAElfModule : AElfModule
    {
        
    }
}