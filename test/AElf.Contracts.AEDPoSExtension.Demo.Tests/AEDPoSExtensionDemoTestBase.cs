using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
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


        internal readonly List<ParliamentContractContainer.ParliamentContractStub> ParliamentStubs =
            new List<ParliamentContractContainer.ParliamentContractStub>();

        public AEDPoSExtensionDemoTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                // You can deploy more system contracts by adding system contract name to current list.
                TokenSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name
            }));
        }

        internal void InitialAcs3Stubs()
        {
            foreach (var initialKeyPair in MissionedECKeyPairs.InitialKeyPairs)
            {
                ParliamentStubs.Add(GetTester<ParliamentContractContainer.ParliamentContractStub>(
                    ContractAddresses[ParliamentSmartContractAddressNameProvider.Name], initialKeyPair));
            }
        }

        internal async Task ParliamentReachAnAgreementAsync(CreateProposalInput createProposalInput)
        {
            var createProposalTx = ParliamentStubs.First().CreateProposal.GetTransaction(createProposalInput);
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                createProposalTx
            });
            var proposalId = new Hash();
            proposalId.MergeFrom(TransactionTraceProvider.GetTransactionTrace(createProposalTx.GetHash()).ReturnValue);
            var approvals = new List<Transaction>();
            foreach (var stub in ParliamentStubs)
            {
                approvals.Add(stub.Approve.GetTransaction(proposalId));
            }

            await BlockMiningService.MineBlockAsync(approvals);

            await ParliamentStubs.First().Release.SendAsync(proposalId);
        }
    }
}