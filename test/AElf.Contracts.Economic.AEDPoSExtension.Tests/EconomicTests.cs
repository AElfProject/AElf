using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    // ReSharper disable once InconsistentNaming
    public class EconomicTests : EconomicTestBase
    {
        [Fact]
        public async Task TreasuryDistributionTest_FirstTerm()
        {
            const int minimumBlocksToChangeTerm =
                AEDPoSExtensionConstants.TimeEachTerm / (AEDPoSExtensionConstants.MiningInterval / 1000);
            const int actualBlocks = minimumBlocksToChangeTerm + 10;
            var treasurySchemeId = await TreasuryStub.GetTreasurySchemeId.CallAsync(new Empty());
            var schemes = await GetCurrentSchemes(treasurySchemeId);
            var minedBlocksInFirstRound = 0L;
            long distributedAmount;
            for (var i = 0; i < actualBlocks; i++)
            {
                await BlockMiningService.MineBlockAsync();
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                if (round.TermNumber == 2)
                {
                    var previousRound = await ConsensusStub.GetPreviousRoundInformation.CallAsync(new Empty());
                    minedBlocksInFirstRound = previousRound.RealTimeMinersInformation.Values.Sum(m => m.ProducedBlocks);
                }
            }

            // Check term number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);
            }

            // Check distributed total amount.
            {
                distributedAmount = minedBlocksInFirstRound * EconomicTestConstants.RewardPerBlock;
                var distributedInformation = await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    SchemeId = treasurySchemeId,
                    Period = 1
                });
                distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: -40% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformation(schemes[SchemeType.MinerBasicReward].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol]; 
                    amount.ShouldBe(-distributedAmount * 2 / 5);
                }
                
                // Backup Subsidy: -20% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformation(schemes[SchemeType.BackupSubsidy].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol]; 
                    amount.ShouldBe(-distributedAmount / 5);
                }

                // Citizen Welfare: -20% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformation(schemes[SchemeType.CitizenWelfare].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol]; 
                    amount.ShouldBe(-distributedAmount / 5);
                }
                
                // Votes Weight Reward: -10% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformation(schemes[SchemeType.VotesWeightReward].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol]; 
                    amount.ShouldBe(-distributedAmount / 10);
                }
                
                // Re-Election Reward: -10% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformation(schemes[SchemeType.ReElectionReward].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol]; 
                    amount.ShouldBe(-distributedAmount / 10);
                }
            }
        }

        private async Task<DistributedProfitsInfo> GetDistributedInformation(Hash schemeId, long period)
        {
            return await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
            {
                SchemeId = schemeId,
                Period = period
            });
        }
        private async Task<Dictionary<SchemeType, Scheme>> GetCurrentSchemes(Hash treasurySchemeId)
        {
            var schemes = new Dictionary<SchemeType, Scheme>();
            var treasuryScheme = await ProfitStub.GetScheme.CallAsync(treasurySchemeId);
            schemes.Add(SchemeType.Treasury, treasuryScheme);
            var minerRewardScheme = await ProfitStub.GetScheme.CallAsync(treasuryScheme.SubSchemes[0].SchemeId);
            schemes.Add(SchemeType.MinerReward, minerRewardScheme);
            schemes.Add(SchemeType.BackupSubsidy,
                await ProfitStub.GetScheme.CallAsync(treasuryScheme.SubSchemes[1].SchemeId));
            schemes.Add(SchemeType.CitizenWelfare,
                await ProfitStub.GetScheme.CallAsync(treasuryScheme.SubSchemes[2].SchemeId));
            schemes.Add(SchemeType.MinerBasicReward,
                await ProfitStub.GetScheme.CallAsync(minerRewardScheme.SubSchemes[0].SchemeId));
            schemes.Add(SchemeType.VotesWeightReward,
                await ProfitStub.GetScheme.CallAsync(minerRewardScheme.SubSchemes[1].SchemeId));
            schemes.Add(SchemeType.ReElectionReward,
                await ProfitStub.GetScheme.CallAsync(minerRewardScheme.SubSchemes[2].SchemeId));
            return schemes;
        }

        private enum SchemeType
        {
            Treasury,

            MinerReward,
            BackupSubsidy,
            CitizenWelfare,

            MinerBasicReward,
            VotesWeightReward,
            ReElectionReward
        }
    }
}