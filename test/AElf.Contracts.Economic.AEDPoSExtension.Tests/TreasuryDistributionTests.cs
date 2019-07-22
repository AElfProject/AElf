using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public partial class EconomicTests : EconomicTestBase
    {
        private readonly Hash _treasurySchemeId;
        private readonly Dictionary<SchemeType, Scheme> _schemes;

        public EconomicTests()
        {
            _schemes = AsyncHelper.RunSync(GetTreasurySchemesAsync);
            _treasurySchemeId = AsyncHelper.RunSync(() => TreasuryStub.GetTreasurySchemeId.CallAsync(new Empty()));
        }

        /// <summary>
        /// Distribute treasury after first term and check each profit scheme.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TreasuryDistributionTest_FirstTerm()
        {
            const int minimumBlocksToChangeTerm =
                AEDPoSExtensionConstants.TimeEachTerm / (AEDPoSExtensionConstants.MiningInterval / 1000);
            const int actualBlocks = minimumBlocksToChangeTerm + 10;
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
                    SchemeId = _treasurySchemeId,
                    Period = 1
                });
                distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: -40% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.MinerBasicReward].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount * 2 / 5);
                }

                // Backup Subsidy: -20% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.BackupSubsidy].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 5);
                }

                // Citizen Welfare: -20% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.CitizenWelfare].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 5);
                }

                // Votes Weight Reward: -10% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.VotesWeightReward].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 10);
                }

                // Re-Election Reward: -10% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.ReElectionReward].SchemeId, 1);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 10);
                }
            }
        }
    }
}