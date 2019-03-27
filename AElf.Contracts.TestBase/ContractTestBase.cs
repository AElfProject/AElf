using System;
using AElf.TestBase;

namespace AElf.Contracts.TestBase
{
    //[Obsolete("Deprecated. Use AElf.Contracts.TestKit for contract testing.")]
    public class ContractTestBase<TContractTestAElfModule> : AElfIntegratedTest<TContractTestAElfModule>
        where TContractTestAElfModule : ContractTestAElfModule
    {
        protected ContractTester<TContractTestAElfModule> Tester { get; set; } = new ContractTester<TContractTestAElfModule>();
    }
}