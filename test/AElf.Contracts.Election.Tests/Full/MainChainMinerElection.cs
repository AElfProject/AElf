using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Profit;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task UserVote_And_GetProfitAmount()
        {
            FullNodesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var moreVotesCandidates = FullNodesKeyPairs.Take(InitialMinersCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

            var lessVotesCandidates = FullNodesKeyPairs.Skip(InitialMinersCount).Take(InitialMinersCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextRound(BootMinerKeyPair);

            var profitTester = GetProfitContractStub(VotersKeyPairs[0]);
            var profitBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
            {
                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                Symbol = "ELF"
            })).Value;
            profitBalance.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}