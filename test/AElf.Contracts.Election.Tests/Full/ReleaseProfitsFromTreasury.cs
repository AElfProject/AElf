using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

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
        var updatedFlexibleReward = 0L;
        var updatedCitizenWelfare = 0L;

        var treasuryScheme =
            await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);

        // Prepare candidates and votes.
        {
            // SampleKeyPairs[13...47] announce election.
            foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

            // Check the count of announce candidates.
            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

            // SampleKeyPairs[13...17] get 2 votes.
            var moreVotesCandidates = ValidationDataCenterKeyPairs
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            foreach (var keyPair in moreVotesCandidates)
                await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 2);

            // SampleKeyPairs[18...30] get 1 votes.
            var lessVotesCandidates = ValidationDataCenterKeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Take(EconomicContractsTestConstants.SupposedMinersCount -
                      EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            foreach (var keyPair in lessVotesCandidates)
                await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 1);

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
                var distributedProfitsInfo =
                    await GetDistributedProfitsInfo(ProfitType.BackupSubsidy, currentPeriod);
                distributedProfitsInfo.TotalShares.ShouldBe(
                    EconomicContractsTestConstants.InitialCoreDataCenterCount * 5);
                distributedProfitsInfo.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
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
                    AEDPoSContractStub.GetPreviousTermInformation.CallAsync(new Int64Value { Value = 1 }).Result;
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                releasedInformation.IsReleased.ShouldBeTrue();
                releasedInformation.TotalShares.ShouldBe(
                    previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks));
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 10 + rewardAmount / 20);
            }

            // Amount of basic reward.
            {
                var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                amount.ShouldBe(0);
            }

            // Flexible reward.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.FlexibleReward, currentPeriod);
                releasedInformation.IsReleased.ShouldBeTrue();
                // Flexible rewards went to 17 new miners.
                releasedInformation.TotalShares.ShouldBe(17);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 20);
            }

            // Amount of flexible reward.
            {
                var amount = await GetProfitAmount(ProfitType.FlexibleReward);
                amount.ShouldBe(rewardAmount / 20 / 17);
                updatedFlexibleReward += rewardAmount / 20 / 17;
            }

            // Welcome reward.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.WelcomeReward, currentPeriod);
                releasedInformation.IsReleased.ShouldBeTrue();
                // Welcome rewards went to Citizen Welfare Reward.
                releasedInformation.TotalShares.ShouldBe(1);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 20);
            }

            // Amount of welcome reward.
            {
                var amount = await GetProfitAmount(ProfitType.WelcomeReward);
                amount.ShouldBe(0);
            }

            // Citizen welfare.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                releasedInformation.IsReleased.ShouldBeTrue();
                releasedInformation.TotalShares.ShouldBe(0);
                // 75% + 5%
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
                    AEDPoSContractStub.GetPreviousTermInformation.CallAsync(new Int64Value { Value = 2 }).Result;
                var totalProducedBlocks =
                    previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks);
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                releasedInformation.TotalShares.ShouldBe(totalProducedBlocks);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 10 + rewardAmount / 20);
                var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                updatedBasicReward += releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol] *
                    previousTermInformation
                        .RealTimeMinersInformation[ValidationDataCenterKeyPairs[0].PublicKey.ToHex()]
                        .ProducedBlocks / totalProducedBlocks;
                amount.ShouldBe(updatedBasicReward);
            }

            // Flexible reward.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.FlexibleReward, currentPeriod);
                // Flexible rewards went to Basic Miner Reward.
                releasedInformation.TotalShares.ShouldBe(1);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 20);
            }

            // Welcome reward.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.WelcomeReward, currentPeriod);
                releasedInformation.IsReleased.ShouldBeTrue();
                releasedInformation.TotalShares.ShouldBe(1);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 20);
            }

            // Amount of welcome reward.
            {
                var amount = await GetProfitAmount(ProfitType.WelcomeReward);
                amount.ShouldBe(0);
            }

            // Citizen welfare.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount * 4 / 5);

                // Amount of citizen welfare.
                var electorVote = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringValue
                    { Value = VoterKeyPairs[0].PublicKey.ToHex() });
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


            // Flexible reward.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.FlexibleReward, currentPeriod);
                // Flexible rewards went to Basic Miner Reward.
                releasedInformation.TotalShares.ShouldBe(1);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 20);
            }

            // Amount of flexible reward.
            {
                var amount = await GetProfitAmount(ProfitType.FlexibleReward);
                amount.ShouldBe(updatedFlexibleReward);
            }

            // Welcome reward.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.WelcomeReward, currentPeriod);
                releasedInformation.IsReleased.ShouldBeTrue();
                releasedInformation.TotalShares.ShouldBe(1);
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 20);
            }

            // Amount of welcome reward.
            {
                var amount = await GetProfitAmount(ProfitType.WelcomeReward);
                amount.ShouldBe(0);
            }

            // Basic reward.
            {
                var previousTermInformation =
                    AEDPoSContractStub.GetPreviousTermInformation.CallAsync(new Int64Value { Value = 3 }).Result;
                var totalProducedBlocks =
                    previousTermInformation.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks);
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.BasicMinerReward, currentPeriod);
                releasedInformation.TotalShares.ShouldBe(totalProducedBlocks);
                // 10% + 5% (from Welcome Reward) + 5% (from Flexible Reward)
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount / 10 + rewardAmount / 20 + rewardAmount / 20);
                var amount = await GetProfitAmount(ProfitType.BasicMinerReward);
                updatedBasicReward += releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol] *
                    previousTermInformation
                        .RealTimeMinersInformation[ValidationDataCenterKeyPairs[0].PublicKey.ToHex()]
                        .ProducedBlocks / totalProducedBlocks;
                amount.ShouldBe(updatedBasicReward);
            }

            // Citizen welfare.
            {
                var releasedInformation =
                    await GetDistributedProfitsInfo(ProfitType.CitizenWelfare, currentPeriod);
                // 75% + 5% (from Flexible Reward)
                releasedInformation.AmountsMap[EconomicContractsTestConstants.NativeTokenSymbol]
                    .ShouldBe(rewardAmount * 3 / 4);

                // Amount of citizen welfare.
                var electorVote = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringValue
                    { Value = VoterKeyPairs[0].PublicKey.ToHex() });
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
                SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare]
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

                //flexible Shares - 10%
                var flexibleRewardWeight = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.FlexibleReward],
                    Symbol = "ELF"
                })).Value;
                flexibleRewardWeight.ShouldBeGreaterThan(0);

                //welcome Shares - 10%
                var welcomeBalance = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.WelcomeReward],
                    Symbol = "ELF"
                })).Value;
                welcomeBalance.ShouldBe(0);

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
                    SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward]
                });
                var profitSize = profitBasicResult.Transaction.Size();
                profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var voteResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.FlexibleReward]
                });
                var voteSize = voteResult.Transaction.Size();
                voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var backupResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy]
                });
                var backSize = backupResult.Transaction.Size();
                backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(miner.PublicKey),
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Balance;
                var sizeFees = (profitSize + voteSize + backSize) * txSizeFeeUnitPrice;
                afterToken.ShouldBe(beforeToken + basicMinerRewardAmount + flexibleRewardWeight +
                    welcomeBalance + backupBalance - txFee * 3 - sizeFees);
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

                //flexible Shares - 75%
                var flexibleRewardWeight = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.FlexibleReward],
                    Symbol = "ELF"
                })).Value;
                flexibleRewardWeight.ShouldBe(0);

                //welcome Shares - 5%
                var welcomeBalance = (await profitTester.GetProfitAmount.CallAsync(new GetProfitAmountInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.WelcomeReward],
                    Symbol = "ELF"
                })).Value;
                welcomeBalance.ShouldBe(0);

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
                    SchemeId = ProfitItemsIds[ProfitType.BasicMinerReward]
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
                    SchemeId = ProfitItemsIds[ProfitType.FlexibleReward]
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
                    balance1.ShouldBe(beforeToken + basicMinerRewardAmount + flexibleRewardWeight
                                      - 2 * txFee - sizeFee);
                }
                {
                    var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;
                    sizeFee = (profitSize + voteSize) * txSizeFeeUnitPrice;
                    balance.ShouldBe(beforeToken + basicMinerRewardAmount + flexibleRewardWeight +
                        welcomeBalance - 2 * txFee - sizeFee);
                }

                var backupResult = await profitTester.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy]
                });
                var backSize = backupResult.Transaction.Size();
                backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                {
                    var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;
                    sizeFee = (profitSize + voteSize + backSize) * txSizeFeeUnitPrice;
                    balance.ShouldBe(beforeToken + basicMinerRewardAmount + flexibleRewardWeight +
                        welcomeBalance + backupBalance - 3 * txFee - sizeFee);
                }
            }
        }
    }

    private async Task GenerateMiningReward(long supposedNextTermNumber)
    {
        for (var i = 0; i < EconomicContractsTestConstants.InitialCoreDataCenterCount; i++)
            await ProduceBlocks(ValidationDataCenterKeyPairs[i], 10);

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

    /// <summary>
    ///     Just to make sure not using double type.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    private long CalculateAverage(List<long> list)
    {
        var sum = list.Sum();
        return sum.Div(list.Count);
    }

    private long CalculateShares(long producedBlocksCount, long averageProducedBlocksCount)
    {
        if (producedBlocksCount < averageProducedBlocksCount.Div(2))
            // If count < (1/2) * average_count, then this node won't share Basic Miner Reward.
            return 0;

        if (producedBlocksCount < averageProducedBlocksCount.Div(5).Mul(4))
            // If count < (4/5) * average_count, then ratio will be (count / average_count)
            return producedBlocksCount.Mul(producedBlocksCount).Div(averageProducedBlocksCount);

        return producedBlocksCount;
    }
}