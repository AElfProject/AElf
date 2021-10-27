using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Contract.Vote
{
    // ReSharper disable once InconsistentNaming
    public class VoteContractTestBase : AEDPoSExtensionTestBase
    {
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);
        
        internal TokenContractContainer.TokenContractStub TokenStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);


        public VoteContractTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                ConsensusSmartContractAddressNameProvider.Name,
                VoteSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name
            }));
        }
    }
}