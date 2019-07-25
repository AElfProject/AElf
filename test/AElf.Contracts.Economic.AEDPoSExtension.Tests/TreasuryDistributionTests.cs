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
            long distributedAmount;

            // First 5 core data centers announce election.
            var announceTransactions = new List<Transaction>();
            foreach (var electionStub in ConvertKeyPairsToElectionStubs(
                MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(5)))
            {
                announceTransactions.Add(electionStub.AnnounceElection.GetTransaction(new Empty()));
            }

            await BlockMiningService.MineBlockAsync(announceTransactions);

            // Check candidates.
            var candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(5);

            // First 10 citizens do some votes.
            var votesTransactions = new List<Transaction>();
            foreach (var candidate in candidates.Value)
            {
                votesTransactions.AddRange(await GetVoteTransactionsAsync(5, 100, candidate.ToHex(), 10));
            }

            await BlockMiningService.MineBlockAsync(votesTransactions);
            
            // Check voted candidates
            var votedCandidates = await ElectionStub.GetVotedCandidates.CallAsync(new Empty());
            votedCandidates.Value.Count.ShouldBe(5);

            var minedBlocksInFirstRound = await MineBlocksToNextTermAsync(1);

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
                    amount.ShouldBe(distributedAmount / 5);
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