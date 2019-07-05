using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken.Messages;
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
            ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var moreVotesCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

            var lessVotesCandidates = ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount).Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            var profitTester = GetProfitContractTester(VoterKeyPairs[0]);
            var profitBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
            {
                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                Symbol = "ELF"
            })).Value;
            var firstPeriodVirtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                    Period = 1
                });
            var releasedBasicMinerRewards = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = firstPeriodVirtualAddress, 
                Symbol = "ELF"
            })).Balance;
            releasedBasicMinerRewards.ShouldBeGreaterThan(0);
            profitBalance.ShouldBeGreaterThan(0);
        }
    }
}