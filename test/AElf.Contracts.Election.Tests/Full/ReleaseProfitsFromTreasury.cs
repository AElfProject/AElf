using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests
    {
        [Fact]
        public async Task CheckTreasuryProfitsDistribution_Test()
        {
            const long txFee = 0;
            const long txSizeFeeUnitPrice = 0;
            long rewardAmount;
            var updatedBackupSubsidy = 0L;
            var updatedBasicReward = 0L;
            var updatedVotesWeightReward = 0L;
            var updatedCitizenWelfare = 0L;

            var treasuryScheme =
                await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);

            // Prepare candidates and votes.
            {
                // SampleKeyPairs[13...47] announce election.
                foreach (var keyPair in ValidationDataCenterKeyPairs)
                {
                    await AnnounceElectionAsync(keyPair);
                }

                // Check the count of announce candidates.
                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

                // SampleKeyPairs[13...17] get 2 votes.
                var moreVotesCandidates = ValidationDataCenterKeyPairs
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                foreach (var keyPair in moreVotesCandidates)
                {
                    await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 2);
                }

                // SampleKeyPairs[18...30] get 1 votes.
                var lessVotesCandidates = ValidationDataCenterKeyPairs
                    .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                    .Take(EconomicContractsTestConstants.SupposedMinersCount - EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                foreach (var keyPair in lessVotesCandidates)
                {
                    await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 1);
                }

                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.SupposedMinersCount);
            }

            // Produce 10 blocks and change term.
            {
                await ProduceBlocks(BootMinerKeyPair, 10);
                await NextTerm(BootMinerKeyPair);
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);
            }

            // Check released profits of first term. No one can receive released profits of first term.
            {
                rewardAmount = await GetReleasedAmount();
                const long currentPeriod = 1L;

                // Check balance of Treasury general ledger.
                {
                    var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = treasuryScheme.VirtualAddress,
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    balance.Balance.ShouldBe(0);
                }

                // Backup subsidy.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BackupSubsidy, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(
                        EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 20);
                }

                // Amount of backup subsidy.
                {
                    var amount = await GetProfitAmount(ProfitType.BackupSubsidy);
                    updatedBackupSubsidy +=
                        rewardAmount / 20 / (EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    amount.ShouldBe(updatedBackupSubsidy);
                }

                // Basic reward.
                {
                    var previousTermInformation =
                        AEDPoSContractStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 1}).Result;
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(
                        previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks));
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 10);
                }

                // Amount of basic reward.
                {
                    var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                    amount.ShouldBe(0);
                }

                // Votes weights reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.VotesWeightReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(0);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 20);
                }

                // Amount of votes weights reward.
                {
                    var amount = await GetProfitAmount(ProfitType.VotesWeightReward);
                    amount.ShouldBe(0);
                }

                // Re-election reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.ReElectionReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(0);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 20);
                }

                // Amount of re-election reward.
                {
                    var amount = await GetProfitAmount(ProfitType.ReElectionReward);
                    amount.ShouldBe(0);
                }

                // Citizen welfare.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(0);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount * 3 / 4);
                }

                // Amount of citizen welfare.
                {
                    var amount = await GetProfitAmount(ProfitType.CitizenWelfare);
                    amount.ShouldBe(0);
                }
            }

            await GenerateMiningReward(3);

            // Check released profits of second term.
            {
                rewardAmount = await GetReleasedAmount();
                const long currentPeriod = 2L;

                // Check balance of Treasury general ledger.
                {
                    var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = treasuryScheme.VirtualAddress,
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    balance.Balance.ShouldBe(0);
                }

                // Backup subsidy.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BackupSubsidy, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(
                        EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 20);
                }

                // Amount of backup subsidy.
                {
                    var amount = await GetProfitAmount(ProfitType.BackupSubsidy);
                    updatedBackupSubsidy +=
                        rewardAmount / 20 / (EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    amount.ShouldBe(updatedBackupSubsidy);
                }

                // Basic reward.
                {
                    var previousTermInformation =
                        AEDPoSContractStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 2}).Result;
                    var totalProducedBlocks =
                        previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks);
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(totalProducedBlocks);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 10);
                    var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                    updatedBasicReward += rewardAmount / 10 *
                                          previousTermInformation
                                              .RealTimeMinersInformation[ValidationDataCenterKeyPairs[0].PublicKey.ToHex()]
                                              .ProducedBlocks / totalProducedBlocks;
                    amount.ShouldBe(updatedBasicReward);
                }

                // Votes weights reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.VotesWeightReward, currentPeriod);
                    // First 5 victories each obtained 2 votes, last 12 victories each obtained 1 vote.
                    releasedInformation.TotalShares.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                                                             EconomicContractsTestConstants.SupposedMinersCount);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 20);
                }

                // Amount of votes weights reward.
                {
                    var amount = await GetProfitAmount(ProfitType.VotesWeightReward);
                    updatedVotesWeightReward +=
                        rewardAmount / 20 * 2 / (EconomicContractsTestConstants.InitialCoreDataCenterCount +
                                                 EconomicContractsTestConstants.SupposedMinersCount);
                    amount.ShouldBe(updatedVotesWeightReward);
                }

                // Re-election reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.ReElectionReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(0);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 20);
                }

                // Amount of re-election reward.
                {
                    var amount = await GetProfitAmount(ProfitType.ReElectionReward);
                    amount.ShouldBe(0);
                }

                // Citizen welfare.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount * 3 / 4);

                    // Amount of citizen welfare.
                    var electorVote = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringValue
                        {Value = VoterKeyPairs[0].PublicKey.ToHex()});
                    var electorWeights = electorVote.ActiveVotingRecords.Sum(r => r.Weight);
                    electorWeights.ShouldBe(releasedInformation.TotalShares);
                    var amount = await GetProfitAmount(ProfitType.CitizenWelfare);
                    updatedCitizenWelfare += electorWeights * rewardAmount * 3 / 4 / releasedInformation.TotalShares;
                    amount.ShouldBeLessThan(updatedCitizenWelfare);
                }
            }

            await GenerateMiningReward(4);

            // Check released profits of third term.
            {
                rewardAmount = await GetReleasedAmount();
                const long currentPeriod = 3L;

                // Check balance of Treasury general ledger.
                {
                    var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = treasuryScheme.VirtualAddress,
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    balance.Balance.ShouldBe(0);
                }

                // Backup subsidy.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BackupSubsidy, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(
                        EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 20);
                }

                // Amount of backup subsidy.
                {
                    var amount = await GetProfitAmount(ProfitType.BackupSubsidy);
                    updatedBackupSubsidy +=
                        rewardAmount / 20 / (EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    amount.ShouldBe(updatedBackupSubsidy);
                }

                // Basic reward.
                {
                    var previousTermInformation =
                        AEDPoSContractStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 3}).Result;
                    var totalProducedBlocks =
                        previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks);
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(totalProducedBlocks);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 10);
                    var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                    updatedBasicReward += rewardAmount / 10 *
                                          previousTermInformation
                                              .RealTimeMinersInformation[ValidationDataCenterKeyPairs[0].PublicKey.ToHex()]
                                              .ProducedBlocks / totalProducedBlocks;
                    amount.ShouldBe(updatedBasicReward);
                }

                // Votes weights reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.VotesWeightReward, currentPeriod);
                    // First 5 victories each obtained 2 votes, last 4 victories each obtained 1 vote.
                    releasedInformation.TotalShares.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                                                             EconomicContractsTestConstants.SupposedMinersCount);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 20);
                }

                // Amount of votes weights reward.
                {
                    var amount = await GetProfitAmount(ProfitType.VotesWeightReward);
                    updatedVotesWeightReward +=
                        rewardAmount / 20 * 2 / (EconomicContractsTestConstants.InitialCoreDataCenterCount +
                                                 EconomicContractsTestConstants.SupposedMinersCount);
                    amount.ShouldBe(updatedVotesWeightReward);
                }

                // Re-election reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.ReElectionReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(EconomicContractsTestConstants.SupposedMinersCount);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 20);
                }

                // Amount of re-election reward.
                {
                    var amount = await GetProfitAmount(ProfitType.ReElectionReward);
                    amount.ShouldBe(rewardAmount / 20 / EconomicContractsTestConstants.SupposedMinersCount);
                }

                // Citizen welfare.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                    releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount * 3 / 4);

                    // Amount of citizen welfare.
                    var electorVote = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringValue
                        {Value = VoterKeyPairs[0].PublicKey.ToHex()});
                    var electorWeights = electorVote.ActiveVotingRecords.Sum(r => r.Weight);
                    electorWeights.ShouldBe(releasedInformation.TotalShares);
                    var amount = await GetProfitAmount(ProfitType.CitizenWelfare);
                    updatedCitizenWelfare += electorWeights * rewardAmount * 3 / 4 / releasedInformation.TotalShares;
                    amount.ShouldBeLessThan(updatedCitizenWelfare);
                }
            }

            //query and profit voter vote profit
            {
                var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(VoterKeyPairs[0].PublicKey),
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Balance;

                var profitTester = GetProfitContractTester(VoterKeyPairs[0]);
                var profitAmount = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare],
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Value;
                profitAmount.ShouldBeGreaterThan(0);

                var profitResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare],
                });
                var txSize = profitResult.Transaction.Size();
                profitResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(VoterKeyPairs[0].PublicKey),
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Balance;
                afterBalance.ShouldBe(beforeBalance + profitAmount - txFee - txSize * txSizeFeeUnitPrice);
            }

            await GenerateMiningReward(5);

            //query and profit miner profit
            {
                foreach (var miner in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants
                    .InitialCoreDataCenterCount))
                {
                    var beforeToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    var profitTester = GetProfitContractTester(miner);

                    //basic Shares - 40%
                    var basicMinerRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = "ELF"
                    })).Value;
                    basicMinerRewardAmount.ShouldBeGreaterThan(0);

                    //vote Shares - 10%
                    var votesWeightRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = "ELF"
                    })).Value;
                    votesWeightRewardAmount.ShouldBeGreaterThan(0);

                    //re-election Shares - 10%
                    var reElectionBalance = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = "ELF"
                    })).Value;
                    reElectionBalance.ShouldBeGreaterThan(0);

                    //backup Shares - 20%
                    var backupBalance = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = "ELF"
                    })).Value;
                    backupBalance.ShouldBeGreaterThan(0);

                    //Profit all
                    var profitBasicResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                    });
                    var profitSize = profitBasicResult.Transaction.Size();
                    profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var voteResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                    });
                    var voteSize = voteResult.Transaction.Size();
                    voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var reElectionResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                    });
                    var reElectionSize = reElectionResult.Transaction.Size();
                    reElectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var backupResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                    });
                    var backSize = backupResult.Transaction.Size();
                    backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var afterToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;
                    var sizeFees = (profitSize + voteSize + reElectionSize + backSize) * txSizeFeeUnitPrice;
                    afterToken.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                        reElectionBalance + backupBalance - txFee * 4 - sizeFees);
                }
            }

            await GenerateMiningReward(6);

            //query and profit miner profit
            {
                foreach (var miner in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants
                    .InitialCoreDataCenterCount))
                {
                    var beforeToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    var profitTester = GetProfitContractTester(miner);

                    //basic Shares - 10%
                    var basicMinerRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = "ELF"
                    })).Value;
                    basicMinerRewardAmount.ShouldBeGreaterThan(0);

                    //vote Shares - 75%
                    var votesWeightRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = "ELF"
                    })).Value;
                    votesWeightRewardAmount.ShouldBeGreaterThan(0);

                    //re-election Shares - 5%
                    var reElectionBalance = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = "ELF"
                    })).Value;
                    reElectionBalance.ShouldBeGreaterThan(0);

                    //backup Shares - 5%
                    var backupBalance = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = "ELF"
                    })).Value;
                    backupBalance.ShouldBeGreaterThan(0);

                    //Profit all
                    var profitBasicResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                    });
                    var profitSize = profitBasicResult.Transaction.Size();
                    profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    long sizeFee;

                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance; 
                        sizeFee = profitSize * txSizeFeeUnitPrice;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount - txFee - sizeFee);
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount - txFee);
                    }

                    var voteResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                    });
                    var voteSize = voteResult.Transaction.Size();
                    voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    {
                        var balance1 = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        sizeFee = (profitSize + voteSize) * txSizeFeeUnitPrice;
                        balance1.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount
                                          - 2 * txFee - sizeFee);
                    }

                    var reElectionResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                    });
                    var reElectionSize = reElectionResult.Transaction.Size();
                    reElectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        sizeFee = (profitSize + voteSize + reElectionSize) * txSizeFeeUnitPrice;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                         reElectionBalance - 3 * txFee - sizeFee);
                    }

                    var backupResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                    });
                    var backSize = backupResult.Transaction.Size();
                    backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        sizeFee = (profitSize + voteSize + reElectionSize + backSize) * txSizeFeeUnitPrice;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                         reElectionBalance + backupBalance - 4 *txFee - sizeFee);
                    }
                }
            }
        }

        private async Task GenerateMiningReward(long supposedNextTermNumber)
        {
            for (var i = 0; i < EconomicContractsTestConstants.InitialCoreDataCenterCount; i++)
            {
                await ProduceBlocks(ValidationDataCenterKeyPairs[i], 10);
            }

            await NextTerm(ValidationDataCenterKeyPairs[0]);

            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.TermNumber.ShouldBe(supposedNextTermNumber);
        }

        private async Task<DistributedProfitsInfo> GetDistributedProfitsInfo(ProfitType type, long period)
        {
            return await ProfitContractStub.GetDistributedProfitsInfo.CallAsync(
                new SchemePeriod
                {
                    SchemeId = ProfitItemsIds[type],
                    Period = period
                });
        }

        private async Task<long> GetProfitAmount(ProfitType type)
        {
            ProfitContractImplContainer.ProfitContractImplStub stub;
            switch (type)
            {
                case ProfitType.CitizenWelfare:
                    stub = GetProfitContractTester(VoterKeyPairs[0]);
                    break;
                default:
                    stub = GetProfitContractTester(ValidationDataCenterKeyPairs[0]);
                    break;
            }

            return (await stub.GetProfitAmount.CallAsync(new GetProfitAmountInput
            {
                SchemeId = ProfitItemsIds[type],
                Symbol = EconomicContractsTestConstants.NativeTokenSymbol
            })).Value;
        }

        private async Task<long> GetReleasedAmount()
        {
            var previousRound = await AEDPoSContractStub.GetPreviousRoundInformation.CallAsync(new Empty());
            var minedBlocks = previousRound.GetMinedBlocks();
            return EconomicContractsTestConstants.ElfTokenPerBlock * minedBlocks;
        }
    }
}