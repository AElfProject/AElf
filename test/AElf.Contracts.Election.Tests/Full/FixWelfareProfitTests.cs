using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests
{
    [Fact(Skip = "Need aelf foundation settings.")]
    public async Task FixWelfareProfitTest()
    {
        foreach (var keyPair in CoreDataCenterKeyPairs)
        {
            await AnnounceElectionAsync(keyPair);
        }

        await VoteToCandidateAsync(VoterKeyPairs[0], CoreDataCenterKeyPairs[0].PublicKey.ToHex(), 15 * 86400,
            10_00000000);

        Hash voteId;
        {
            var profitDetail =
                await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[0].PublicKey));
            profitDetail.Details[0].Id.ShouldNotBeNull();
            profitDetail.Details[0].StartPeriod.ShouldBe(2);
            profitDetail.Details[0].EndPeriod.ShouldBe(3);
            voteId = profitDetail.Details[0].Id;
        }
        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        await ChangeVotingOption(VoterKeyPairs[0], CoreDataCenterKeyPairs[1].PublicKey.ToHex(),
            voteId, true);
        
        await NextTerm(BootMinerKeyPair);
        await NextTerm(BootMinerKeyPair);
        await NextTerm(BootMinerKeyPair);

        var executionResult = await ElectionContractStub.FixWelfareProfit.SendAsync(new FixWelfareProfitInput
        {
            FixInfoList =
            {
                new FixWelfareProfitInfo
                {
                    VoteId = voteId,
                    StartPeriod = 2,
                    EndPeriod = 7
                }
            }
        });
        executionResult.TransactionResult.Error.ShouldBeNullOrEmpty();

        {
            var profitDetail =
                await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[0].PublicKey));
            profitDetail.Details[0].Id.ShouldNotBeNull();
            profitDetail.Details[0].StartPeriod.ShouldBe(2);
            profitDetail.Details[0].EndPeriod.ShouldBe(7);
        }
    }
}