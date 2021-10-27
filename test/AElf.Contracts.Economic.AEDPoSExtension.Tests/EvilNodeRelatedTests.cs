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
using AElf.Kernel.Proposal;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public class EvilNodeRelatedTests : EconomicTestBase
    {
        [Fact(Skip = "Need fix.")]
        internal async Task MarkEvilNodeTest()
        {
            UpdateParliamentStubs(MissionedECKeyPairs.InitialKeyPairs);
            var newCandidates = MissionedECKeyPairs.ValidationDataCenterKeyPairs.Take(18).ToList();
            await NodesAnnounceElection(newCandidates);
            await BlockMiningService.MineBlockToNextTermAsync();

            var miners = newCandidates.Take(17).ToList();
            //UpdateParliamentStubs(miners);

            await BlockMiningService.MineBlockToNextRoundAsync();

            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ParliamentSmartContractAddressNameProvider.Name],
                ContractMethodName = "CreateEmergencyResponseOrganization",
                Params = new Empty().ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });
            var eroAddress =
                await ParliamentStubs.First().GetEmergencyResponseOrganizationAddress.CallAsync(new Empty());
            var evilNodePubkey = MissionedECKeyPairs.ValidationDataCenterKeyPairs.First().PublicKey.ToHex();
            await EmergencyResponseOrganizationReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ElectionSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(ElectionStub.RemoveEvilNode),
                Params = new StringValue
                {
                    Value = evilNodePubkey
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = eroAddress
            });
            await BlockMiningService.MineBlockToNextRoundAsync();
            miners.Remove(MissionedECKeyPairs.ValidationDataCenterKeyPairs.First());
            miners.Add(MissionedECKeyPairs.ValidationDataCenterKeyPairs.Skip(17).Take(1).First());
            UpdateParliamentStubs(miners);

            var currentRound = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RealTimeMinersInformation.Keys.ShouldNotContain(evilNodePubkey);
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