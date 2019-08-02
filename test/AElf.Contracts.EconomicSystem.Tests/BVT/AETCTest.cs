using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken.Messages;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public void ElectionContract_CreateTreasury_Test()
        {
            // Check profit items related to Treasury.
            // Theses items already created during AElf Consensus Contract initialization,
            // and cached in ElectionContractTestBase.InitializeContracts in order to test.
            ProfitItemsIds.Count.ShouldBe(7);
        }
        
        [Fact]
        public async Task ElectionContract_RegisterToTreasury_Test()
        {
            var treasury = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            // MinerReward (Shares 3) -> Treasury
            treasury.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitItemsIds[ProfitType.MinerReward]);
            treasury.SubSchemes.First(s => s.SchemeId == ProfitItemsIds[ProfitType.MinerReward]).Shares
                .ShouldBe(ElectionContractConstants.MinerRewardWeight);
            // BackupSubsidy (Shares 1) -> Treasury
            treasury.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitItemsIds[ProfitType.BackupSubsidy]);
            treasury.SubSchemes.First(s => s.SchemeId == ProfitItemsIds[ProfitType.BackupSubsidy]).Shares
                .ShouldBe(ElectionContractConstants.BackupSubsidyWeight);
            // CitizenWelfare (Shares 1) -> Treasury
            treasury.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitItemsIds[ProfitType.CitizenWelfare]);
            treasury.SubSchemes.First(s => s.SchemeId == ProfitItemsIds[ProfitType.CitizenWelfare]).Shares
                .ShouldBe(ElectionContractConstants.CitizenWelfareWeight);

            var reward = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
            // BasicMinerReward (Shares 4) -> Reward
            reward.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitItemsIds[ProfitType.BasicMinerReward]);
            reward.SubSchemes.First(s => s.SchemeId == ProfitItemsIds[ProfitType.BasicMinerReward]).Shares
                .ShouldBe(ElectionContractConstants.BasicMinerRewardWeight);
            // VotesWeightReward (Shares 1) -> Reward
            reward.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitItemsIds[ProfitType.VotesWeightReward]);
            reward.SubSchemes.First(s => s.SchemeId == ProfitItemsIds[ProfitType.VotesWeightReward]).Shares
                .ShouldBe(ElectionContractConstants.VotesWeightRewardWeight);
            // ReElectionReward (Shares 1) -> Reward
            reward.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitItemsIds[ProfitType.ReElectionReward]);
            reward.SubSchemes.First(s => s.SchemeId == ProfitItemsIds[ProfitType.ReElectionReward]).Shares
                .ShouldBe(ElectionContractConstants.ReElectionRewardWeight);

            // Check the balance of Treasury
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = treasury.VirtualAddress,
                Symbol = "VOTE"
            });
            balance.Balance.ShouldBe(0);
        }
    }
}