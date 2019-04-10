using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.TestBase;
using AElf.Types.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.TestKit
{
    public class ContractTestBase<TContractTestAElfModule> : AElfIntegratedTest<TContractTestAElfModule>
        where TContractTestAElfModule : ContractTestModule
    {
        public T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            var factory = Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
            return factory.Create<T>(contractAddress, senderKey);
        }
    }
}