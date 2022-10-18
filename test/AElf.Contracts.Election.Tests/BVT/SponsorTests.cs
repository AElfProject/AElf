using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests
{
    [Fact]
    public async Task ElectionContract_AnnounceElectionFor_State_Test()
    {
        var candidatesKeyPair = ValidationDataCenterKeyPairs.First();
        var sponsorKeyPair = ValidationDataCenterKeyPairs.Last();
        var balanceBeforeAnnouncing = await GetNativeTokenBalance(sponsorKeyPair.PublicKey);

        // Execute AnnounceElectionFor.
        var electionStub = GetElectionContractTester(sponsorKeyPair);
        var candidateAdmin = Address.FromPublicKey(candidatesKeyPair.PublicKey);
        await electionStub.AnnounceElectionFor.SendAsync(new AnnounceElectionForInput
        {
            Admin = candidateAdmin,
            Pubkey = candidatesKeyPair.PublicKey.ToHex()
        });

        var balanceAfterAnnouncing = await GetNativeTokenBalance(sponsorKeyPair.PublicKey);
        balanceAfterAnnouncing.ShouldBe(balanceBeforeAnnouncing - ElectionContractConstants.LockTokenForElection);

        var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
        {
            VotingItemId = MinerElectionVotingItemId
        });
        votingItem.Options.Count.ShouldBe(1);
        votingItem.Options.ShouldContain(candidatesKeyPair.PublicKey.ToHex());
        var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
        dataCenterList.DataCenters.ContainsKey(candidatesKeyPair.PublicKey.ToHex()).ShouldBeTrue();
        var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
        var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
        {
            SchemeId = subsidy,
            Beneficiary = Address.FromPublicKey(candidatesKeyPair.PublicKey)
        });
        profitDetail.Details.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ElectionContract_QuitElection_Sponsor_Test()
    {
        await ElectionContract_AnnounceElectionFor_State_Test();

        var candidatesKeyPair = ValidationDataCenterKeyPairs.First();
        var sponsorKeyPair = ValidationDataCenterKeyPairs.Last();
        var balanceBeforeAnnouncing = await GetNativeTokenBalance(sponsorKeyPair.PublicKey);

        await QuitElectionAsync(candidatesKeyPair);

        var balanceAfterAnnouncing = await GetNativeTokenBalance(sponsorKeyPair.PublicKey);
        balanceAfterAnnouncing.ShouldBe(balanceBeforeAnnouncing + ElectionContractConstants.LockTokenForElection);
    }
}