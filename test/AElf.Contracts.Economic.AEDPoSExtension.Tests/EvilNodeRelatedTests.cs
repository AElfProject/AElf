using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Contracts.Election;
using AElf.ContractTestKit;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.GovernmentSystem;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public class EvilNodeRelatedTests : EconomicTestBase
    {
        [Fact(Skip = "Need to find another way to mark someone as evil node.")]
        public async Task MarkEvilNodeTest()
        {
            UpdateParliamentStubs(MissionedECKeyPairs.InitialKeyPairs);
            var newCandidates = MissionedECKeyPairs.ValidationDataCenterKeyPairs.Take(18).ToList();
            await NodesAnnounceElection(newCandidates);
            await BlockMiningService.MineBlockToNextTermAsync();
            UpdateParliamentStubs(newCandidates.Take(17));
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            var evilNodePubkey = MissionedECKeyPairs.ValidationDataCenterKeyPairs.First().PublicKey.ToHex();
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ElectionSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(ElectionStub.UpdateCandidateInformation),
                Params = new UpdateCandidateInformationInput
                {
                    Pubkey = evilNodePubkey,
                    IsEvilNode = true
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });
            await BlockMiningService.MineBlockToNextRoundAsync();
//            await BlockMiningService.MineBlockToNextRoundAsync();
//            var currentRound = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
//            currentRound.RealTimeMinersInformation.Keys.ShouldNotContain(evilNodePubkey);
        }

        private async Task NodesAnnounceElection(List<ECKeyPair> nodeAccounts)
        {
            var candidateElectionStubs = nodeAccounts.Select(keyPair =>
                GetTester<ElectionContractContainer.ElectionContractStub>(
                    ContractAddresses[ElectionSmartContractAddressNameProvider.Name], keyPair)).ToList();
            foreach (var electionStub in candidateElectionStubs)
            {
                await electionStub.AnnounceElection.SendAsync(SampleAccount.Accounts.First().Address);
            }

            foreach (var keyPair in nodeAccounts)
            {
                await candidateElectionStubs.First().Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey = keyPair.PublicKey.ToHex(),
                    Amount = 100,
                    EndTimestamp = TimestampHelper.GetUtcNow().AddSeconds(100)
                });
            }
        }
    }
}