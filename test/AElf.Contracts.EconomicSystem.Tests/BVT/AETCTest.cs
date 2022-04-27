using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public void ElectionContract_CreateTreasury_Test()
        {
            // Check profit schemes related to Treasury.
            // Theses items already created during AElf Consensus Contract initialization,
            // and cached in ElectionContractTestBase.InitializeContracts in order to test.
            ProfitSchemeIdList.Count.ShouldBe(7);
        }
        
        [Fact]
        public async Task ElectionContract_RegisterToTreasury_Test()
        {
            var treasury = await ProfitContractStub.GetScheme.CallAsync(ProfitSchemeIdList[ProfitType.Treasury]);
            // MinerReward (Shares 3) -> Treasury
            treasury.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitSchemeIdList[ProfitType.MinerReward]);
            treasury.SubSchemes.First(s => s.SchemeId == ProfitSchemeIdList[ProfitType.MinerReward]).Shares
                .ShouldBe(ElectionContractConstants.MinerRewardWeight);
            // BackupSubsidy (Shares 1) -> Treasury
            treasury.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitSchemeIdList[ProfitType.BackupSubsidy]);
            treasury.SubSchemes.First(s => s.SchemeId == ProfitSchemeIdList[ProfitType.BackupSubsidy]).Shares
                .ShouldBe(ElectionContractConstants.BackupSubsidyWeight);
            // CitizenWelfare (Shares 1) -> Treasury
            treasury.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitSchemeIdList[ProfitType.CitizenWelfare]);
            treasury.SubSchemes.First(s => s.SchemeId == ProfitSchemeIdList[ProfitType.CitizenWelfare]).Shares
                .ShouldBe(ElectionContractConstants.CitizenWelfareWeight);

            var reward = await ProfitContractStub.GetScheme.CallAsync(ProfitSchemeIdList[ProfitType.MinerReward]);
            // BasicMinerReward (Shares 4) -> Reward
            reward.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitSchemeIdList[ProfitType.BasicMinerReward]);
            reward.SubSchemes.First(s => s.SchemeId == ProfitSchemeIdList[ProfitType.BasicMinerReward]).Shares
                .ShouldBe(ElectionContractConstants.BasicMinerRewardWeight);
            // VotesWeightReward (Shares 1) -> Reward
            reward.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitSchemeIdList[ProfitType.FlexibleReward]);
            reward.SubSchemes.First(s => s.SchemeId == ProfitSchemeIdList[ProfitType.FlexibleReward]).Shares
                .ShouldBe(ElectionContractConstants.FlexibleRewardWeight);
            // ReElectionReward (Shares 1) -> Reward
            reward.SubSchemes.Select(s => s.SchemeId).ShouldContain(ProfitSchemeIdList[ProfitType.WelcomeReward]);
            reward.SubSchemes.First(s => s.SchemeId == ProfitSchemeIdList[ProfitType.WelcomeReward]).Shares
                .ShouldBe(ElectionContractConstants.WelcomeRewardWeight);

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