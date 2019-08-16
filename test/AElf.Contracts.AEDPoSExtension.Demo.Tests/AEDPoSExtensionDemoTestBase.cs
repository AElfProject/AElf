using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtensionDemoTestBase : AEDPoSExtensionTestBase
    {
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal TokenContractContainer.TokenContractStub TokenStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        public AEDPoSExtensionDemoTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                // You can deploy more system contracts by adding system contract name to current list.
                TokenSmartContractAddressNameProvider.Name
            }));
        }
    }
}