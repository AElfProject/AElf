using System.Collections.Generic;
using AElf.Contracts.Deployer;
using AElf.TestBase;

namespace AElf.Contracts.TestBase
{
    public class ContractTestBase<TContractTestAElfModule> : AElfIntegratedTest<TContractTestAElfModule>
        where TContractTestAElfModule : ContractTestAElfModule
    {
        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<TContractTestAElfModule>());
        protected ContractTester<TContractTestAElfModule> Tester { get; set; } = new ContractTester<TContractTestAElfModule>();
    }
}