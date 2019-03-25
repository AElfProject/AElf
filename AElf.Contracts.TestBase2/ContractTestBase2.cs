using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Cryptography.ECDSA;
using AElf.Modularity;
using AElf.TestBase;
using AElf.Types.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.TestBase2
{
    public class ContractTestBase2<TContractTestAElfModule> : AElfIntegratedTest<TContractTestAElfModule>
        where TContractTestAElfModule : ContractTestAElfModule2
    {
        public T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractTesterBase, new()
        {
            var factory = Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
            return factory.Create<T>(contractAddress, senderKey);
        }
    }
}