using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.TestBase;
using Volo.Abp.Threading;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainContractTestBase : AElfIntegratedTest<CrossChainContractTestAElfModule>
    {
        protected ContractTester ContractTester;
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        public CrossChainContractTestBase()
        {
            ContractTester = new ContractTester(0, CrossChainContractTestHelper.EcKeyPair);
            AsyncHelper.RunSync(() => ContractTester.InitialChainAsync(ContractTester.GetDefaultContractTypes().ToArray()));
            CrossChainContractAddress = ContractTester.DeployedContractsAddresses[(int) ContractConsts.CrossChainContract];
            TokenContractAddress = ContractTester.DeployedContractsAddresses[(int) ContractConsts.TokenContract];
        }
    }
}