using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.Profit;
using AElf.ContractTestKit;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public partial class EconomicTests
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
        [IgnoreOnCIFact]
        public async Task<long> TreasuryDistribution_FirstTerm_Test()
        {
            const long period = 1;
            long distributedAmount;
            
            //Without candidate announce election
            var candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(0);
            
            // First 7 core data centers announce election.
            var announceTransactions = new List<Transaction>();
            ConvertKeyPairsToElectionStubs(
                MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(7)).ForEach(stub =>
                announceTransactions.Add(stub.AnnounceElection.GetTransaction(SampleAccount.Accounts.First().Address)));
            await BlockMiningService.MineBlockAsync(announceTransactions);

            // Check candidates.
            candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(7);

            // First 10 citizens do some votes.
            var votesTransactions = new List<Transaction>();
            candidates.Value.ToList().ForEach(c =>
                votesTransactions.AddRange(GetVoteTransactions(5, 100, c.ToHex(), 10)));
            await BlockMiningService.MineBlockAsync(votesTransactions);

            // Check voted candidates
            var votedCandidates = await ElectionStub.GetVotedCandidates.CallAsync(new Empty());
            votedCandidates.Value.Count.ShouldBe(7);

            var minedBlocksInFirstRound = await MineBlocksToNextTermAsync(1);

            // Check new term.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);

                // Now we have 12 miners.
                var currentMiners = await ConsensusStub.GetCurrentMinerList.CallAsync(new Empty());
                currentMiners.Pubkeys.Count.ShouldBe(12);
                // And none of the initial miners was replaced.
                MissionedECKeyPairs.InitialKeyPairs.Select(p => p.PublicKey.ToHex())
                    .Except(currentMiners.Pubkeys.Select(p => p.ToHex())).Count().ShouldBe(0);
            }

            // Check distributed total amount.
            {
                distributedAmount = minedBlocksInFirstRound * EconomicTestConstants.RewardPerBlock;
                var distributedInformation = await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    SchemeId = _treasurySchemeId,
                    Period = period
                });
                distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: 10%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.MinerBasicReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 10);
                }

                // Backup Subsidy: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.BackupSubsidy].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                }

                // Citizen Welfare: -75% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.CitizenWelfare].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount * 3 / 4);
                }

                // Votes Weight Reward: -5% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.VotesWeightReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 20);
                }

                // Re-Election Reward: -5% (Burned)
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.ReElectionReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(-distributedAmount / 20);
                }
                return distributedAmount;
            }
        }

        [IgnoreOnCIFact]
        public async Task<TreasuryDistributionInformation> TreasuryDistribution_SecondTerm_Test()
        {
            var information = new TreasuryDistributionInformation();
            const long period = 2;
            long distributedAmount;

            var termNumber = (await ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value;
            if (termNumber < 2)
            {
                await TreasuryDistribution_FirstTerm_Test();
            }

            // Remain 10 core data centers announce election.
            var announceTransactions = new List<Transaction>();
            ConvertKeyPairsToElectionStubs(
                MissionedECKeyPairs.CoreDataCenterKeyPairs.Skip(7).Take(10)).ForEach(stub =>
                announceTransactions.Add(stub.AnnounceElection.GetTransaction(SampleAccount.Accounts.First().Address)));
            await BlockMiningService.MineBlockAsync(announceTransactions);

            // Check candidates.
            var candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(AEDPoSExtensionConstants.CoreDataCenterKeyPairCount);

            // First 10 citizens do some votes.
            var votesTransactions = new List<Transaction>();
            candidates.Value.ToList().ForEach(c =>
                votesTransactions.AddRange(GetVoteTransactions(5, 100, c.ToHex(), 10)));
            await BlockMiningService.MineBlockAsync(votesTransactions);

            // Check voted candidates
            var votedCandidates = await ElectionStub.GetVotedCandidates.CallAsync(new Empty());
            votedCandidates.Value.Count.ShouldBe(AEDPoSExtensionConstants.CoreDataCenterKeyPairCount);

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
                distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);

                information.TotalAmount = distributedAmount;
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: 10%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.MinerBasicReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 10);
                    var totalShares = distributedInformation.TotalShares;
                    var previousTermInformation =
                        ConsensusStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 2}).Result;
                    totalShares.ShouldBe(
                        previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks));

                    information[SchemeType.MinerBasicReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Backup Subsidy: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.BackupSubsidy].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(AEDPoSExtensionConstants.CoreDataCenterKeyPairCount);

                    information[SchemeType.BackupSubsidy] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Citizen Welfare: 75%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.CitizenWelfare].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount * 3 / 4);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBePositive();

                    information[SchemeType.CitizenWelfare] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Votes Weight Reward: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.VotesWeightReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(7000);

                    information[SchemeType.VotesWeightReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Re-Election Reward: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.ReElectionReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(5);

                    information[SchemeType.ReElectionReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }
            }

            return information;
        }

        [Fact]
        public async Task<TreasuryDistributionInformation> TreasuryDistribution_ThirdTerm_Test()
        {
            var information = new TreasuryDistributionInformation();
            const long period = 3;
            long distributedAmount;

            var termNumber = (await ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value;
            if (termNumber < 3)
            {
                await TreasuryDistribution_SecondTerm_Test();
            }

            // 10 validation data centers announce election.
            var announceTransactions = new List<Transaction>();
            ConvertKeyPairsToElectionStubs(
                MissionedECKeyPairs.ValidationDataCenterKeyPairs.Take(10)).ForEach(stub =>
                announceTransactions.Add(
                    stub.AnnounceElection.GetTransaction(Address.FromPublicKey(Accounts[0].KeyPair.PublicKey))));
            await BlockMiningService.MineBlockAsync(announceTransactions);

//            await BlockMiningService.MineBlockAsync(new List<Transaction>
//            {
//                ElectionStub.ReplaceCandidatePubkey.GetTransaction(new ReplaceCandidatePubkeyInput
//                {
//                    OldPubkey = MissionedECKeyPairs.CoreDataCenterKeyPairs.Skip(6).First().PublicKey.ToHex(),
//                    NewPubkey = MissionedECKeyPairs.ValidationDataCenterKeyPairs.Last().PublicKey.ToHex()
//                })
//            });

            // Check candidates.
            var candidates = await ElectionStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(27);

            // First 10 citizens do some votes.
            var votesTransactions = new List<Transaction>();
            candidates.Value.ToList().ForEach(c =>
                votesTransactions.AddRange(GetVoteTransactions(5, 100, c.ToHex(), 20)));
            await BlockMiningService.MineBlockAsync(votesTransactions);

            // Check voted candidates
            var votedCandidates = await ElectionStub.GetVotedCandidates.CallAsync(new Empty());
            votedCandidates.Value.Count.ShouldBe(27);

            var minedBlocksInFirstRound = await MineBlocksToNextTermAsync(3);

            // Check term number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(4);
            }

            // Check distributed total amount.
            {
                distributedAmount = minedBlocksInFirstRound * EconomicTestConstants.RewardPerBlock;
                var distributedInformation = await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    SchemeId = _treasurySchemeId,
                    Period = period
                });
                distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol].ShouldBe(distributedAmount);

                information.TotalAmount = distributedAmount;
            }

            // Check amount distributed to each scheme.
            {
                // Miner Basic Reward: 10%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.MinerBasicReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 10);
                    var totalShares = distributedInformation.TotalShares;
                    var previousTermInformation =
                        ConsensusStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 3}).Result;
                    totalShares.ShouldBe(
                        previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks));

                    information[SchemeType.MinerBasicReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Backup Subsidy: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.BackupSubsidy].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(27);

                    information[SchemeType.BackupSubsidy] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Citizen Welfare: 75%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.CitizenWelfare].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount * 3 / 4);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBePositive();

                    information[SchemeType.CitizenWelfare] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Votes Weight Reward: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.VotesWeightReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(7000 + 17000);

                    information[SchemeType.VotesWeightReward] = new DistributionInformation
                    {
                        Amount = amount,
                        TotalShares = totalShares
                    };
                }

                // Re-Election Reward: 5%
                {
                    var distributedInformation =
                        await GetDistributedInformationAsync(_schemes[SchemeType.ReElectionReward].SchemeId, period);
                    var amount = distributedInformation.AmountsMap[EconomicTestConstants.TokenSymbol];
                    amount.ShouldBe(distributedAmount / 20);
                    var totalShares = distributedInformation.TotalShares;
                    totalShares.ShouldBe(11);

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