using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests
{
    [Fact]
    public async Task ChangeVotingOptionTest()
    {
        foreach (var keyPair in CoreDataCenterKeyPairs)
        {
            await AnnounceElectionAsync(keyPair);
        }

        // Term 1
        // Voter 1 votes 3 times.
        for (var i = 0; i < 3; i++)
        {
            await VoteToCandidateAsync(VoterKeyPairs[0], CoreDataCenterKeyPairs[0].PublicKey.ToHex(), 20 * 86400,
                10);
        }

        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        // Term 2
        // Change voting option for first vote.
        {
            var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
            {
                Value = VoterKeyPairs[0].PublicKey.ToHex()
            });
            await ChangeVotingOption(VoterKeyPairs[0], CoreDataCenterKeyPairs[0].PublicKey.ToHex(),
                electorVotes.ActiveVotingRecordIds.First(), true);
        }
        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        // Term 4
        // Change voting option for second vote.
        {
            var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
            {
                Value = VoterKeyPairs[0].PublicKey.ToHex()
            });
            await ChangeVotingOption(VoterKeyPairs[0], CoreDataCenterKeyPairs[0].PublicKey.ToHex(),
                electorVotes.ActiveVotingRecordIds.Skip(1).First(), true);
        }
        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        // Term 6
        // Change voting option for last vote.
        {
            var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
            {
                Value = VoterKeyPairs[0].PublicKey.ToHex()
            });
            await ChangeVotingOption(VoterKeyPairs[0], CoreDataCenterKeyPairs[0].PublicKey.ToHex(),
                electorVotes.ActiveVotingRecordIds.Last(), true);
        }
        await ProduceBlocks(BootMinerKeyPair, 10);
        await NextTerm(BootMinerKeyPair);

        {
            var profitDetails =
                await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[0].PublicKey));
            profitDetails.Details.Count.ShouldBe(3);
        }

        // Term 7
        for (var i = 0; i < 5; i++)
        {
            await ClaimProfitsAsync(VoterKeyPairs[0]);
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);
        }
    }
}