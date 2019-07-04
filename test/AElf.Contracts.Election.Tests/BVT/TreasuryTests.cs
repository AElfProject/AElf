using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Cryptography.ECDSA;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public void ElectionContract_CreateTreasury()
        {
            // Check profit items related to Treasury.
            // Theses items already created during AElf Consensus Contract initialization,
            // and cached in ElectionContractTestBase.InitializeContracts in order to test.
            ProfitItemsIds.Count.ShouldBe(7);
        }

        [Fact]
        public async Task ElectionContract_RegisterToTreasury()
        {
            var treasury = await ProfitContractStub.GetProfitItem.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            // MinerReward (weight 3) -> Treasury
            treasury.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.MinerReward]);
            treasury.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.MinerReward]).Weight
                .ShouldBe(ElectionContractConstants.MinerRewardWeight);
            // BackupSubsidy (weight 1) -> Treasury
            treasury.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.BackupSubsidy]);
            treasury.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.BackupSubsidy]).Weight
                .ShouldBe(ElectionContractConstants.BackupSubsidyWeight);
            // CitizenWelfare (weight 1) -> Treasury
            treasury.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.CitizenWelfare]);
            treasury.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.CitizenWelfare]).Weight
                .ShouldBe(ElectionContractConstants.CitizenWelfareWeight);

            var reward = await ProfitContractStub.GetProfitItem.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
            // BasicMinerReward (weight 4) -> Reward
            reward.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.BasicMinerReward]);
            reward.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.BasicMinerReward]).Weight
                .ShouldBe(ElectionContractConstants.BasicMinerRewardWeight);
            // VotesWeightReward (weight 1) -> Reward
            reward.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.VotesWeightReward]);
            reward.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.VotesWeightReward]).Weight
                .ShouldBe(ElectionContractConstants.VotesWeightRewardWeight);
            // ReElectionReward (weight 1) -> Reward
            reward.SubProfitItems.Select(s => s.ProfitId).ShouldContain(ProfitItemsIds[ProfitType.ReElectionReward]);
            reward.SubProfitItems.First(s => s.ProfitId == ProfitItemsIds[ProfitType.ReElectionReward]).Weight
                .ShouldBe(ElectionContractConstants.ReElectionRewardWeight);

            // Check the balance of Treasury
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = treasury.VirtualAddress,
                Symbol = ElectionContractTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(ElectionContractConstants.VotesTotalSupply);
        }

        /// <summary>
        /// In first term, new miners elected.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_ReleaseTreasuryProfits_ReleaseFirstTerm()
        {
            var candidatesKeyPairs = await ElectionContract_Vote();

            await NextRound(BootMinerKeyPair);

            await ProduceBlocks(BootMinerKeyPair, 10, true);

            var profitItems = new Dictionary<ProfitType, ProfitItem>();
            foreach (var (profitType, profitId) in ProfitItemsIds)
            {
                var profitItem = await ProfitContractStub.GetProfitItem.CallAsync(profitId);
                profitItems.Add(profitType, profitItem);
            }

            // Check current period of each profit item.
            //     Citizen welfare release 1 period later.
            profitItems.Values.Where(i => i.VirtualAddress != profitItems[ProfitType.CitizenWelfare].VirtualAddress)
                .ShouldAllBe(i => i.CurrentPeriod == 2);
            profitItems[ProfitType.CitizenWelfare].CurrentPeriod.ShouldBe(1);

            var releasedAmount =
                ElectionContractConstants.VotesTotalSupply - profitItems[ProfitType.Treasury].TotalAmount;
            var totalWeightsOfTreasury = ElectionContractConstants.MinerRewardWeight
                .Add(ElectionContractConstants.BackupSubsidyWeight)
                .Add(ElectionContractConstants.CitizenWelfareWeight);
            var totalWeightsOfMinerReward = ElectionContractConstants.BasicMinerRewardWeight
                .Add(ElectionContractConstants.VotesWeightRewardWeight)
                .Add(ElectionContractConstants.ReElectionRewardWeight);

            // Check profit items status after term 1.
            // Current term number: 2
            {
                // Check BackupSubsidy.
                {
                    // Already released to receiving profits address.
                    profitItems[ProfitType.BackupSubsidy].TotalAmount.ShouldBe(0);

                    // Each candidate takes 1 weight.
                    profitItems[ProfitType.BackupSubsidy].TotalWeight.ShouldBe(candidatesKeyPairs.Count);

                    var releasedProfitsInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                        new GetReleasedProfitsInformationInput
                        {
                            ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                            Period = 1
                        });
                    releasedProfitsInformation.TotalWeight.ShouldBe(candidatesKeyPairs.Count);
                    releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.MinerRewardWeight)
                        .Div(totalWeightsOfTreasury)
                        .Mul(ElectionContractConstants.BackupSubsidyWeight)
                        .Div(ElectionContractConstants.MinerRewardWeight));
                    releasedProfitsInformation.IsReleased.ShouldBe(true);

                    // Check balance of virtual address.
                    {
                        var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                            new GetProfitItemVirtualAddressInput
                            {
                                ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                                Period = 1
                            });
                        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = virtualAddress,
                            Symbol = ElectionContractTestConstants.NativeTokenSymbol
                        });
                        balance.Balance.ShouldBe(releasedProfitsInformation.ProfitsAmount);
                    }
                }

                // Check CitizenWelfare.
                {
                    // Already released to receiving profits address (though no one can really receive this amount of profits).
                    profitItems[ProfitType.CitizenWelfare].TotalAmount.ShouldBe(0);

                    var releasedProfitsInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                        new GetReleasedProfitsInformationInput
                        {
                            ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                            Period = 1
                        });
                    releasedProfitsInformation.TotalWeight.ShouldBe(-1);
                    releasedProfitsInformation.ProfitsAmount.ShouldBe(-1);
                    releasedProfitsInformation.IsReleased.ShouldBe(false);

                    // Check balance of virtual address.
                    {
                        var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                            new GetProfitItemVirtualAddressInput
                            {
                                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                                Period = -1
                            });
                        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = virtualAddress,
                            Symbol = ElectionContractTestConstants.NativeTokenSymbol
                        });
                        balance.Balance.ShouldBe(releasedAmount
                            .Mul(ElectionContractConstants.CitizenWelfareWeight)
                            .Div(totalWeightsOfTreasury));
                    }
                }

                var actualMinersRewardAmount = 0L;
                // Check MinerReward
                {
                    // Check BasicMinerReward
                    {
                        // Already burned.
                        profitItems[ProfitType.BasicMinerReward].TotalAmount.ShouldBe(0);
                        // Each new miner takes 1 weight.
                        profitItems[ProfitType.BasicMinerReward].TotalWeight.ShouldBeLessThan(candidatesKeyPairs.Count);

                        // Check released profit information.
                        // We don't give initial miners rewards.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                                        Period = 1
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(-1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(-1);
                            releasedProfitsInformation.IsReleased.ShouldBe(false);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                                    Period = -1
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.BasicMinerRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Check VotesWeightReward
                    {
                        // Already released to receiving profits address.
                        profitItems[ProfitType.VotesWeightReward].TotalAmount.ShouldBe(0);
                        // The only receiver is Virtual Address of Treasury.
                        profitItems[ProfitType.VotesWeightReward].TotalWeight.ShouldBe(1);

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                                        Period = 1
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(-1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(-1);
                            releasedProfitsInformation.IsReleased.ShouldBe(false);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                                    Period = -1
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.VotesWeightRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Check ReElectionReward
                    {
                        profitItems[ProfitType.ReElectionReward].TotalAmount.ShouldBe(0);
                        // The only receiver is Virtual Address of Treasury.
                        profitItems[ProfitType.ReElectionReward].TotalWeight.ShouldBe(1);

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                        Period = 1
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(-1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(-1);
                            releasedProfitsInformation.IsReleased.ShouldBe(false);
                        }

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                        Period = 1
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(-1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(-1);
                            releasedProfitsInformation.IsReleased.ShouldBe(false);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                    Period = -1
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.ReElectionRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Already released to sub profit items.
                    profitItems[ProfitType.MinerReward].TotalAmount.ShouldBe(0);
                    profitItems[ProfitType.MinerReward].TotalWeight.ShouldBe(totalWeightsOfMinerReward);
                    actualMinersRewardAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.MinerRewardWeight).Div(totalWeightsOfTreasury));
                }
            }

            return candidatesKeyPairs;
        }

        /// <summary>
        /// In second term, new miners do the mining instead of initial miners.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_ReleaseTreasuryProfits_ReleaseSecondTerm()
        {
            var candidatesKeyPairs = await ElectionContract_ReleaseTreasuryProfits_ReleaseFirstTerm();

            const long amount = 1000;
            const int lockTime = 100 * 60 * 60 * 24;

            await VoteToCandidates(VotersKeyPairs.Take(1).ToList(),
                candidatesKeyPairs.Select(kp => kp.PublicKey.ToHex()).Skip(2).ToList(), lockTime, amount);

            await NextRound(candidatesKeyPairs[0]);

            await ProduceBlocks(candidatesKeyPairs[0], 10, true);

            // Update profitItems
            var profitItems = new Dictionary<ProfitType, ProfitItem>();
            foreach (var (profitType, profitId) in ProfitItemsIds)
            {
                var profitItem = await ProfitContractStub.GetProfitItem.CallAsync(profitId);
                profitItems.Add(profitType, profitItem);
            }

            // Update releasedAmount
            var releasedAmount =
                (ElectionContractConstants.VotesTotalSupply - profitItems[ProfitType.Treasury].TotalAmount) / 2;
            var totalWeightsOfTreasury = ElectionContractConstants.MinerRewardWeight
                .Add(ElectionContractConstants.BackupSubsidyWeight)
                .Add(ElectionContractConstants.CitizenWelfareWeight);
            var totalWeightsOfMinerReward = ElectionContractConstants.BasicMinerRewardWeight
                .Add(ElectionContractConstants.VotesWeightRewardWeight)
                .Add(ElectionContractConstants.ReElectionRewardWeight);

            // Check current period of each profit item.
            profitItems.Values.Where(i => i.VirtualAddress != profitItems[ProfitType.CitizenWelfare].VirtualAddress)
                .ShouldAllBe(i => i.CurrentPeriod == 3);
            profitItems[ProfitType.CitizenWelfare].CurrentPeriod.ShouldBe(2);

            // Check profit items status after term 2.
            // Current term number: 3
            {
                // Check BackupSubsidy.
                {
                    // Already released to receiving profits address.
                    profitItems[ProfitType.BackupSubsidy].TotalAmount.ShouldBe(0);

                    // Each candidate takes 1 weight.
                    profitItems[ProfitType.BackupSubsidy].TotalWeight.ShouldBe(candidatesKeyPairs.Count);

                    var releasedProfitsInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                        new GetReleasedProfitsInformationInput
                        {
                            ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                            Period = 2
                        });
                    releasedProfitsInformation.TotalWeight.ShouldBe(candidatesKeyPairs.Count);
                    releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.MinerRewardWeight)
                        .Div(totalWeightsOfTreasury)
                        .Mul(ElectionContractConstants.BackupSubsidyWeight)
                        .Div(ElectionContractConstants.MinerRewardWeight));
                    releasedProfitsInformation.IsReleased.ShouldBe(true);

                    // Check balance of virtual address.
                    {
                        var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                            new GetProfitItemVirtualAddressInput
                            {
                                ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                                Period = 2
                            });
                        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = virtualAddress,
                            Symbol = ElectionContractTestConstants.NativeTokenSymbol
                        });
                        balance.Balance.ShouldBe(releasedProfitsInformation.ProfitsAmount);
                    }
                }

                // Check CitizenWelfare.
                {
                    // Already released to receiving profits address (though no one can really receive this amount of profits).
                    profitItems[ProfitType.CitizenWelfare].TotalAmount.ShouldBe(0);

                    var releasedProfitsInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                        new GetReleasedProfitsInformationInput
                        {
                            ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                            Period = 1
                        });
                    releasedProfitsInformation.TotalWeight.ShouldNotBe(0);
                    releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.CitizenWelfareWeight)
                        .Div(totalWeightsOfTreasury));
                    releasedProfitsInformation.IsReleased.ShouldBe(true);

                    // Check balance of virtual address.
                    {
                        var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                            new GetProfitItemVirtualAddressInput
                            {
                                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                                Period = 1
                            });
                        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = virtualAddress,
                            Symbol = ElectionContractTestConstants.NativeTokenSymbol
                        });
                        balance.Balance.ShouldBe(releasedAmount
                            .Mul(ElectionContractConstants.CitizenWelfareWeight)
                            .Div(totalWeightsOfTreasury));
                    }
                }

                var actualMinersRewardAmount = 0L;
                // Check MinerReward
                {
                    // Check BasicMinerReward
                    {
                        // Already burned.
                        profitItems[ProfitType.BasicMinerReward].TotalAmount.ShouldBe(0);
                        // Each new miner takes 1 weight.
                        profitItems[ProfitType.BasicMinerReward].TotalWeight.ShouldBe(9);

                        // Check released profit information.
                        // We don't give initial miners rewards.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                                        Period = 2
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBeLessThan(candidatesKeyPairs.Count);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.BasicMinerRewardWeight)
                                .Div(totalWeightsOfMinerReward));
                            releasedProfitsInformation.IsReleased.ShouldBe(true);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                                    Period = 2
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.BasicMinerRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Check VotesWeightReward
                    {
                        // Already released to receiving profits address.
                        profitItems[ProfitType.VotesWeightReward].TotalAmount.ShouldBe(0);
                        // The only receiver is Virtual Address of Treasury.
                        profitItems[ProfitType.VotesWeightReward].TotalWeight.ShouldBeGreaterThan(0);

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                                        Period = 2
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.VotesWeightRewardWeight)
                                .Div(totalWeightsOfMinerReward));
                            releasedProfitsInformation.IsReleased.ShouldBe(true);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                                    Period = 2
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.VotesWeightRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Check ReElectionReward
                    {
                        profitItems[ProfitType.ReElectionReward].TotalAmount.ShouldBe(0);
                        // 3 miners re-elected.
                        profitItems[ProfitType.ReElectionReward].TotalWeight.ShouldBe(7);

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                        Period = 2
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.ReElectionRewardWeight)
                                .Div(totalWeightsOfMinerReward));
                            releasedProfitsInformation.IsReleased.ShouldBe(true);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                    Period = 2
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.ReElectionRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Already released to sub profit items.
                    profitItems[ProfitType.MinerReward].TotalAmount.ShouldBe(0);
                    profitItems[ProfitType.MinerReward].TotalWeight.ShouldBe(totalWeightsOfMinerReward);
                    actualMinersRewardAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.MinerRewardWeight).Div(totalWeightsOfTreasury));
                }
            }
            return candidatesKeyPairs;
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasuryProfits_ReleaseThirdTerm()
        {
            var candidatesKeyPairs = await ElectionContract_ReleaseTreasuryProfits_ReleaseSecondTerm();

            await NextRound(candidatesKeyPairs[3]);

            await ProduceBlocks(candidatesKeyPairs[3], 10, true);

            // Update profitItems
            var profitItems = new Dictionary<ProfitType, ProfitItem>();
            foreach (var (profitType, profitId) in ProfitItemsIds)
            {
                var profitItem = await ProfitContractStub.GetProfitItem.CallAsync(profitId);
                profitItems.Add(profitType, profitItem);
            }

            // Update releasedAmount
            var releasedAmount = ElectionContractConstants.VotesTotalSupply -
                                 profitItems[ProfitType.Treasury].TotalAmount -
                                 20 * ElectionContractConstants.ElfTokenPerBlock;
            var totalWeightsOfTreasury = ElectionContractConstants.MinerRewardWeight
                .Add(ElectionContractConstants.BackupSubsidyWeight)
                .Add(ElectionContractConstants.CitizenWelfareWeight);
            var totalWeightsOfMinerReward = ElectionContractConstants.BasicMinerRewardWeight
                .Add(ElectionContractConstants.VotesWeightRewardWeight)
                .Add(ElectionContractConstants.ReElectionRewardWeight);

            // Check current period of each profit item.
            profitItems.Values.Where(i => i.VirtualAddress != profitItems[ProfitType.CitizenWelfare].VirtualAddress)
                .ShouldAllBe(i => i.CurrentPeriod == 4);
            profitItems[ProfitType.CitizenWelfare].CurrentPeriod.ShouldBe(3);

            // Check profit items status after term 3.
            // Current term number: 4
            {
                // Check BackupSubsidy.
                {
                    // Already released to receiving profits address.
                    profitItems[ProfitType.BackupSubsidy].TotalAmount.ShouldBe(0);

                    // Each candidate takes 1 weight.
                    profitItems[ProfitType.BackupSubsidy].TotalWeight.ShouldBe(candidatesKeyPairs.Count);

                    var releasedProfitsInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                        new GetReleasedProfitsInformationInput
                        {
                            ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                            Period = 3
                        });
                    releasedProfitsInformation.TotalWeight.ShouldBe(candidatesKeyPairs.Count);
                    releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.MinerRewardWeight)
                        .Div(totalWeightsOfTreasury)
                        .Mul(ElectionContractConstants.BackupSubsidyWeight)
                        .Div(ElectionContractConstants.MinerRewardWeight));
                    releasedProfitsInformation.IsReleased.ShouldBe(true);

                    // Check balance of virtual address.
                    {
                        var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                            new GetProfitItemVirtualAddressInput
                            {
                                ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                                Period = 3
                            });
                        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = virtualAddress,
                            Symbol = ElectionContractTestConstants.NativeTokenSymbol
                        });
                        balance.Balance.ShouldBe(releasedProfitsInformation.ProfitsAmount);
                    }
                }

                // Check CitizenWelfare.
                {
                    // Already released to receiving profits address (though no one can really receive this amount of profits).
                    profitItems[ProfitType.CitizenWelfare].TotalAmount.ShouldBe(0);

                    var releasedProfitsInformation = await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                        new GetReleasedProfitsInformationInput
                        {
                            ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                            Period = 2
                        });
                    releasedProfitsInformation.TotalWeight.ShouldNotBe(0);
                    releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.CitizenWelfareWeight)
                        .Div(totalWeightsOfTreasury));
                    releasedProfitsInformation.IsReleased.ShouldBe(true);

                    // Check balance of virtual address.
                    {
                        var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                            new GetProfitItemVirtualAddressInput
                            {
                                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                                Period = 2
                            });
                        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                        {
                            Owner = virtualAddress,
                            Symbol = ElectionContractTestConstants.NativeTokenSymbol
                        });
                        balance.Balance.ShouldBe(releasedAmount
                            .Mul(ElectionContractConstants.CitizenWelfareWeight)
                            .Div(totalWeightsOfTreasury));
                    }
                }

                var actualMinersRewardAmount = 0L;
                // Check MinerReward
                {
                    // Check BasicMinerReward
                    {
                        // Already burned.
                        profitItems[ProfitType.BasicMinerReward].TotalAmount.ShouldBe(0);
                        // Each new miner takes 1 weight.
                        profitItems[ProfitType.BasicMinerReward].TotalWeight.ShouldBe(9);

                        // Check released profit information.
                        // We don't give initial miners rewards.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                                        Period = 3
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(profitItems[ProfitType.BasicMinerReward]
                                .TotalWeight);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.BasicMinerRewardWeight)
                                .Div(totalWeightsOfMinerReward));
                            releasedProfitsInformation.IsReleased.ShouldBe(true);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                                    Period = 3
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.BasicMinerRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Check VotesWeightReward
                    {
                        // Already released to receiving profits address.
                        profitItems[ProfitType.VotesWeightReward].TotalAmount.ShouldBe(0);
                        // The only receiver is Virtual Address of Treasury.
                        profitItems[ProfitType.VotesWeightReward].TotalWeight.ShouldBeGreaterThan(0);

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                                        Period = 3
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBeGreaterThan(1);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.VotesWeightRewardWeight)
                                .Div(totalWeightsOfMinerReward));
                            releasedProfitsInformation.IsReleased.ShouldBe(true);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                                    Period = 3
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.VotesWeightRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Check ReElectionReward
                    {
                        profitItems[ProfitType.ReElectionReward].TotalAmount.ShouldBe(0);
                        // 3 miners got re-elected twice, 2 miners once.
                        profitItems[ProfitType.ReElectionReward].TotalWeight.ShouldBe(7 * 2 + 2 * 1);

                        // Check released profit information.
                        {
                            var releasedProfitsInformation =
                                await ProfitContractStub.GetReleasedProfitsInformation.CallAsync(
                                    new GetReleasedProfitsInformationInput
                                    {
                                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                        Period = 3
                                    });
                            releasedProfitsInformation.TotalWeight.ShouldBe(7);
                            releasedProfitsInformation.ProfitsAmount.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.ReElectionRewardWeight)
                                .Div(totalWeightsOfMinerReward));
                            releasedProfitsInformation.IsReleased.ShouldBe(true);
                        }

                        // Check balance of virtual address.
                        {
                            var virtualAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                                new GetProfitItemVirtualAddressInput
                                {
                                    ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                                    Period = 3
                                });
                            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                            {
                                Owner = virtualAddress,
                                Symbol = ElectionContractTestConstants.NativeTokenSymbol
                            });
                            balance.Balance.ShouldBe(releasedAmount
                                .Mul(ElectionContractConstants.MinerRewardWeight)
                                .Div(totalWeightsOfTreasury)
                                .Mul(ElectionContractConstants.ReElectionRewardWeight)
                                .Div(totalWeightsOfMinerReward));

                            actualMinersRewardAmount += balance.Balance;
                        }
                    }

                    // Already released to sub profit items.
                    profitItems[ProfitType.MinerReward].TotalAmount.ShouldBe(0);
                    profitItems[ProfitType.MinerReward].TotalWeight.ShouldBe(totalWeightsOfMinerReward);
                    actualMinersRewardAmount.ShouldBe(releasedAmount
                        .Mul(ElectionContractConstants.MinerRewardWeight).Div(totalWeightsOfTreasury));
                }
            }
        }
    }
}