using AElf.TestBase;

namespace AElf.Contracts.TestBase
{
    public class ContractTestBase<TContractTestAElfModule> : AElfIntegratedTest<TContractTestAElfModule>
        where TContractTestAElfModule : ContractTestAElfModule
    {
        protected ContractTester<TContractTestAElfModule> Tester { get; set; } = new ContractTester<TContractTestAElfModule>();
    }
}