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
        [Fact(Skip = "This test case also run in TreasuryCollectionTest_SecondTerm.")]
        public async Task<long> TreasuryDistributionTest_FirstTerm()
        {
            const long period = 1;
            long distributedAmount;

            // First 5 core data centers announce election.
            var announceTransactions = new List<Transaction>();
            ConvertKeyPairsToElectionStubs(
                MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(5)).ForEach(stub =>
                announceTransactions.Add(stub.AnnounceElection.GetTransaction(new Empty())));
            await BlockMiningService.MineBlockAsync(announceTransactions);

            // Check candidates.
            var candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(5);

            // First 10 citizens do some votes.
            var votesTransactions = new List<Transaction>();
            candidates.Value.ToList().ForEach(async c =>
                votesTransactions.AddRange(await GetVoteTransactionsAsync(5, 100, c.ToHex(), 10)));
            await BlockMiningService.MineBlockAsync(votesTransactions);

            // Check voted candidates
            var votedCandidates = await ElectionStub.GetVotedCandidates.CallAsync(new Empty());
            votedCandidates.Value.Count.ShouldBe(5);

            var minedBlocksInFirstRound = await MineBlocksToNextTermAsync(1);

            // Check new term.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);

                // Now we have 9 miners.
                var currentMiners = await ConsensusStub.GetCurrentMinerList.CallAsync(new Empty());
                currentMiners.Pubkeys.Count.ShouldBe(9);
                // And one of the initial miners was replaced.
                MissionedECKeyPairs.InitialKeyPairs.Select(p => p.PublicKey.ToHex())
                    .Except(currentMiners.Pubkeys.Select(p => p.ToHex())).Count().ShouldBe(1);
            }

            // Check distributed total amount.
            {
                distributedAmount = minedBlocksInFirstRound * EconomicTestConstants.RewardPerBlock;
                var distributedInformation = await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    SchemeId = _treasurySchemeId,
                    Period = period
                });
                distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: -40% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.MinerBasicReward].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount * 2 / 5);
                }

                // Backup Subsidy: 20%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.BackupSubsidy].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 5);
                }

                // Citizen Welfare: -20% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.CitizenWelfare].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 5);
                }

                // Votes Weight Reward: -10% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.VotesWeightReward].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 10);
                }

                // Re-Election Reward: -10% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.ReElectionReward].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 10);
                }
                return distributedAmount;
            }
        }

        [Fact(Skip = "This test case also run in TreasuryCollectionTest_SecondTerm.")]
        public async Task<TreasuryDistributionInformation> TreasuryDistributionTest_SecondTerm()
        {
            var information = new TreasuryDistributionInformation();
            const long period = 2;
            long distributedAmount;

            var termNumber = (await ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value;
            if (termNumber == 1)
            {
                await TreasuryDistributionTest_FirstTerm();
            }

            // Remain 4 core data centers announce election.
            var announceTransactions = new List<Transaction>();
            ConvertKeyPairsToElectionStubs(
                MissionedECKeyPairs.CoreDataCenterKeyPairs.Skip(5).Take(4)).ForEach(stub =>
                announceTransactions.Add(stub.AnnounceElection.GetTransaction(new Empty())));
            await BlockMiningService.MineBlockAsync(announceTransactions);

            // Check candidates.
            var candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(9);

            // First 10 citizens do some votes.
            var votesTransactions = new List<Transaction>();
            candidates.Value.ToList().ForEach(async c =>
                votesTransactions.AddRange(await GetVoteTransactionsAsync(5, 100, c.ToHex(), 10)));
            await BlockMiningService.MineBlockAsync(votesTransactions);

            // Check voted candidates
            var votedCandidates = await ElectionStub.GetVotedCandidates.CallAsync(new Empty());
            votedCandidates.Value.Count.ShouldBe(9);

            var minedBlocksInFirstRound = await MineBlocksToNextTermAsync(2);

            // Check term number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(3);
            }

            // Check distributed total amount.
            {
                distributedAmount = minedBlocksInFirstRound * EconomicTestConstants.RewardPerBlock;
                var distributedInformation = await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    SchemeId = _treasurySchemeId,
                    Period = period
                });
                distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);

                information.TotalAmount = distributedAmount;
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: 40%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.MinerBasicReward].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount * 2 / 5);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(9);

                    information[SchemeType.MinerBasicReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Backup Subsidy: 20%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.BackupSubsidy].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 5);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(9);

                    information[SchemeType.BackupSubsidy] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Citizen Welfare: 20%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.CitizenWelfare].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 5);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBePositive();

                    information[SchemeType.CitizenWelfare] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Votes Weight Reward: 10%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.VotesWeightReward].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 10);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(5000);

                    information[SchemeType.VotesWeightReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Re-Election Reward: 10%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.ReElectionReward].SchemeId, period);
                    var amount = distributedInformation.ProfitsAmount[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 10);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(4);

                    information[SchemeType.ReElectionReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }
            }

            return information;
        }
    }
}