using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
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
            const long txFee = 1_00000000L;
            long rewardAmount;
            var updatedBackupSubsidy = 0L;
            var updatedBasicReward = 0L;
            var updatedVotesWeightReward = 0L;
            var updatedReElectionReward = 0L;
            var updatedCitizenWelfare = 0L;

            var treasuryScheme =
                await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);

            // Prepare candidates and votes.
            {
                // SampleKeyPairs[13...47] announce election.
                ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

                // Check the count of announce candidates.
                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

                // SampleKeyPairs[13...17] get 2 votes.
                var moreVotesCandidates = ValidationDataCenterKeyPairs
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                moreVotesCandidates.ForEach(async kp =>
                    await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

                // SampleKeyPairs[18...22] get 1 votes.
                var lessVotesCandidates = ValidationDataCenterKeyPairs
                    .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                lessVotesCandidates.ForEach(async kp =>
                    await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

                // Check the count of voted candidates, should be 10.
                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount * 2);
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
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 5);
                }

                // Amount of backup subsidy.
                {
                    var amount = await GetProfitAmount(ProfitType.BackupSubsidy);
                    updatedBackupSubsidy +=
                        rewardAmount / 5 / (EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    amount.ShouldBe(updatedBackupSubsidy);
                }

                // Basic reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(0);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount * 2 / 5);
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
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 10);
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
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 10);
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
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 5);
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
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 5);
                }

                // Amount of backup subsidy.
                {
                    var amount = await GetProfitAmount(ProfitType.BackupSubsidy);
                    updatedBackupSubsidy +=
                        rewardAmount / 5 / (EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    amount.ShouldBe(updatedBackupSubsidy);
                }

                // Basic reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(9);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount * 2 / 5);
                }

                // Amount of basic reward.
                {
                    var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                    updatedBasicReward += rewardAmount * 2 / 5 / 9;
                    amount.ShouldBe(updatedBasicReward);
                }

                // Votes weights reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.VotesWeightReward, currentPeriod);
                    // First 5 victories each obtained 2 votes, last 4 victories each obtained 1 vote.
                    releasedInformation.TotalShares.ShouldBe(2 * 5 + 4);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 10);
                }

                // Amount of votes weights reward.
                {
                    var amount = await GetProfitAmount(ProfitType.VotesWeightReward);
                    updatedVotesWeightReward += rewardAmount / 10 * 2 / 14;
                    amount.ShouldBe(updatedVotesWeightReward);
                }

                // Re-election reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.ReElectionReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(0);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(-rewardAmount / 10);
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
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 5);

                    // Amount of citizen welfare.
                    var electorVote = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringInput
                        {Value = VoterKeyPairs[0].PublicKey.ToHex()});
                    var electorWeights = electorVote.ActiveVotingRecords.Sum(r => r.Weight);
                    electorWeights.ShouldBe(releasedInformation.TotalShares);
                    var amount = await GetProfitAmount(ProfitType.CitizenWelfare);
                    updatedCitizenWelfare += electorWeights * rewardAmount / 5 / releasedInformation.TotalShares;
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
                    releasedInformation.TotalShares.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 5);
                }

                // Amount of backup subsidy.
                {
                    var amount = await GetProfitAmount(ProfitType.BackupSubsidy);
                    updatedBackupSubsidy +=
                        rewardAmount / 5 / (EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                    amount.ShouldBe(updatedBackupSubsidy);
                }

                // Basic reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                    releasedInformation.TotalShares.ShouldBe(9);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount * 2 / 5);
                }

                // Amount of basic reward.
                {
                    var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                    updatedBasicReward += rewardAmount * 2 / 5 / 9;
                    amount.ShouldBe(updatedBasicReward);
                }

                // Votes weights reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.VotesWeightReward, currentPeriod);
                    // First 5 victories each obtained 2 votes, last 4 victories each obtained 1 vote.
                    releasedInformation.TotalShares.ShouldBe(2 * 5 + 4);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 10);
                }

                // Amount of votes weights reward.
                {
                    var amount = await GetProfitAmount(ProfitType.VotesWeightReward);
                    updatedVotesWeightReward += rewardAmount / 10 * 2 / 14;
                    amount.ShouldBe(updatedVotesWeightReward);
                }

                // Re-election reward.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.ReElectionReward, currentPeriod);
                    releasedInformation.IsReleased.ShouldBeTrue();
                    releasedInformation.TotalShares.ShouldBe(9);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 10);
                }

                // Amount of re-election reward.
                {
                    var amount = await GetProfitAmount(ProfitType.ReElectionReward);
                    amount.ShouldBe(rewardAmount / 10 / 9);
                }

                // Citizen welfare.
                {
                    var releasedInformation =
                        await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                    releasedInformation.ProfitsAmount[EconomicContractsTestConstants.NativeTokenSymbol]
                        .ShouldBe(rewardAmount / 5);

                    // Amount of citizen welfare.
                    var electorVote = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringInput
                        {Value = VoterKeyPairs[0].PublicKey.ToHex()});
                    var electorWeights = electorVote.ActiveVotingRecords.Sum(r => r.Weight);
                    electorWeights.ShouldBe(releasedInformation.TotalShares);
                    var amount = await GetProfitAmount(ProfitType.CitizenWelfare);
                    updatedCitizenWelfare += electorWeights * rewardAmount / 5 / releasedInformation.TotalShares;
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
                var profitAmount = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare],
                    Symbol = "ELF"
                })).Value;
                profitAmount.ShouldBeGreaterThan(0);

                var profitResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare],
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                });
                profitResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(VoterKeyPairs[0].PublicKey),
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Balance;
                afterBalance.ShouldBe(beforeBalance + profitAmount - txFee);
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
                    var basicMinerRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = "ELF"
                    })).Value;
                    basicMinerRewardAmount.ShouldBeGreaterThan(0);

                    //vote Shares - 10%
                    var votesWeightRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = "ELF"
                    })).Value;
                    votesWeightRewardAmount.ShouldBeGreaterThan(0);

                    //re-election Shares - 10%
                    var reElectionBalance = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = "ELF"
                    })).Value;
                    reElectionBalance.ShouldBeGreaterThan(0);

                    //backup Shares - 20%
                    var backupBalance = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = "ELF"
                    })).Value;
                    backupBalance.ShouldBeGreaterThan(0);

                    //Profit all
                    var profitBasicResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var voteResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var reElectionResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    reElectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var backupResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var afterToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    afterToken.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                        reElectionBalance + backupBalance - txFee * 4);
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

                    //basic Shares - 40%
                    var basicMinerRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = "ELF"
                    })).Value;
                    basicMinerRewardAmount.ShouldBeGreaterThan(0);

                    //vote Shares - 10%
                    var votesWeightRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = "ELF"
                    })).Value;
                    votesWeightRewardAmount.ShouldBeGreaterThan(0);

                    //re-election Shares - 10%
                    var reElectionBalance = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = "ELF"
                    })).Value;
                    reElectionBalance.ShouldBeGreaterThan(0);

                    //backup Shares - 20%
                    var backupBalance = (await profitTester.GetProfitAmount.CallAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = "ELF"
                    })).Value;
                    backupBalance.ShouldBeGreaterThan(0);

                    //Profit all
                    var profitBasicResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    
                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount - txFee);
                    }

                    var voteResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    
                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount - txFee * 2);
                    }

                    var reElectionResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    reElectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    
                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                         reElectionBalance - txFee * 3);
                    }

                    var backupResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                    {
                        SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    
                    {
                        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = Address.FromPublicKey(miner.PublicKey),
                            Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                        })).Balance;
                        balance.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                         reElectionBalance + backupBalance - txFee * 4);
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

            await NextTerm(InitialCoreDataCenterKeyPairs[0]);

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
            ProfitContractContainer.ProfitContractStub stub;
            switch (type)
            {
                case ProfitType.CitizenWelfare:
                    stub = GetProfitContractTester(VoterKeyPairs[0]);
                    break;
                default:
                    stub = GetProfitContractTester(ValidationDataCenterKeyPairs[0]);
                    break;
            }

            return (await stub.GetProfitAmount.CallAsync(new ClaimProfitsInput
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