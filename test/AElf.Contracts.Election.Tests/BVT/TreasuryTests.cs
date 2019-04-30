using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task ElectionContract_CheckReleasedProfits()
        {
            await ElectionContract_GetVictories_ValidCandidatesEnough();

            await NextRound(BootMinerKeyPair);

            await ProduceBlocks(BootMinerKeyPair, 10, true);

            // Check Treasury situation.
            var profitItems = new Dictionary<ProfitType, ProfitItem>();
            foreach (var profitId in ProfitItemsIds)
            {
                var profitItem = await ProfitContractStub.GetProfitItem.CallAsync(profitId.Value);
                profitItems.Add(profitId.Key, profitItem);
            }

            profitItems.Values.ShouldAllBe(i => i.CurrentPeriod == 2);

            var releasedAmount =
                ElectionContractConsts.VotesTotalSupply - profitItems[ProfitType.Treasury].TotalAmount;
            var actualMinersRewardAmount = profitItems[ProfitType.BasicMinerReward].TotalAmount +
                                           profitItems[ProfitType.VotesWeightReward].TotalAmount +
                                           profitItems[ProfitType.ReElectionReward].TotalAmount;
            actualMinersRewardAmount.ShouldBe(releasedAmount * 6 / 10);

            // Check released information of CitizenWelfare of period 1.
            {
                var releasedProfitInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                    new GetReleasedProfitsInformationInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                        Period = 1
                    });
                releasedProfitInformation.ProfitsAmount.ShouldBe(200);
                releasedProfitInformation.IsReleased.ShouldBe(true);
            }

            // Check released information of CitizenWelfare of period 2.
            {
                var releasedProfitInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                    new GetReleasedProfitsInformationInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                        Period = 2
                    });
                releasedProfitInformation.ProfitsAmount.ShouldBe(0);
                releasedProfitInformation.IsReleased.ShouldBe(false);
            }
        }

        [Fact]
        public async Task ElectionContract_GetCandidateHistory()
        {
            const int roundCount = 5;

            var minerKeyPair = FullNodesKeyPairs[0];

            await ElectionContract_GetVictories_ValidCandidatesEnough();

            await NextTerm(BootMinerKeyPair);

            await ProduceBlocks(minerKeyPair, roundCount, true);

            var history = await ElectionContractStub.GetCandidateHistory.CallAsync(new StringInput
            {
                Value = minerKeyPair.PublicKey.ToHex()
            });

            history.PublicKey.ShouldBe(minerKeyPair.PublicKey.ToHex());
        }
    }
}