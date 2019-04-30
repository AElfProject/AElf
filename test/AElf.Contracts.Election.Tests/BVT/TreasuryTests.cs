using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public void ElectionContract_CreateTreasury()
        {
            // Check profit items related to Treasury.
            // Theses items already created during AElf Consensus Contract initialization,
            // and cached in ElectionContractTestBase.InitializeContracts in order to test.
            ProfitItemsIds.Count.ShouldBe(7);
        }

        [Fact]
        public async Task ElectionContract_RegisterToTreasury()
        {
            var treasury = await ProfitContractStub.GetProfitItem.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            // MinerReward (weight 60) -> Treasury
            treasury.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.MinerReward]);
            treasury.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.MinerReward]).Weight.ShouldBe(60);
            // BackupSubsidy (weight 20) -> Treasury
            treasury.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.BackSubsidy]);
            treasury.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.BackSubsidy]).Weight.ShouldBe(20);
            // CitizenWelfare (weight 20) -> Treasury
            treasury.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.CitizenWelfare]);
            treasury.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.CitizenWelfare]).Weight.ShouldBe(20);

            var reward = await ProfitContractStub.GetProfitItem.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
            // BasicMinerReward (weight 4) -> Reward
            reward.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.BasicMinerReward]);
            reward.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.BasicMinerReward]).Weight.ShouldBe(4);
            // VotesWeightReward (weight 1) -> Reward
            reward.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.VotesWeightReward]);
            reward.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.VotesWeightReward]).Weight.ShouldBe(1);
            // ReElectionReward (weight 1) -> Reward
            reward.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.ReElectionReward]);
            reward.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.ReElectionReward]).Weight.ShouldBe(1);
            
            // Check the balance of Treasury
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = treasury.VirtualAddress,
                Symbol = ElectionContractTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(ElectionContractConstants.VotesTotalSupply);
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasuryProfits()
        {
            await ElectionContract_Vote();

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
                ElectionContractConstants.VotesTotalSupply - profitItems[ProfitType.Treasury].TotalAmount;
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
    }
}