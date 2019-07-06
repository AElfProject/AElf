using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task UserVote_And_GetProfitAmount()
        {
            ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

            var moreVotesCandidates = ValidationDataCenterKeyPairs
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

            {
                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            }

            var lessVotesCandidates = ValidationDataCenterKeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            {
                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount * 2);
            }

            {
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(1);
            }

            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            {
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);
            }
            
            await ProduceBlocks(ValidationDataCenterKeyPairs[0], 10);
            await NextTerm(ValidationDataCenterKeyPairs[0]);

            {
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(3);
            }

            var profitTester = GetProfitContractTester(VoterKeyPairs[0]);
            var profitBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
            {
                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                Symbol = "ELF"
            })).Value;
            profitBalance.ShouldBe(25000000);
        }
    }
}