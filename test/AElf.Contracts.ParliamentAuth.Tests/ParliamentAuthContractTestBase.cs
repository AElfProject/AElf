using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthContractTestBase : ContractTestBase<ParliamentAuthContractTestAElfModule>
    {
        protected Address ParliamentAddress;
        protected Address TokenContractAddress;

        protected long _totalSupply;
        protected long _balanceOfStarter;

        protected ContractTester<ParliamentAuthContractTestAElfModule> otherTester;
        protected ContractTester<ParliamentAuthContractTestAElfModule> minerTester;
        
        public ParliamentAuthContractTestBase()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply, out _,
                    out _balanceOfStarter)));

            ParliamentAddress = Tester.GetContractAddress(ParliamentAuthContractAddressNameProvider.Name);
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);

            otherTester = Tester.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
            minerTester = Tester.CreateNewContractTester(Tester.InitialMinerList[0]);
        }
    }
}