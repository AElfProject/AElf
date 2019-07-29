using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit
{
    public class ProfitTests : ProfitContractTestBase
    {
        public ProfitTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ProfitContract_CheckTreasury()
        {
            await CreateTreasury();

            var treasury = await ProfitContractStub.GetProfitItem.CallAsync(TreasuryHash);

            treasury.Creator.ShouldBe(Address.FromPublicKey(StarterKeyPair.PublicKey));
            treasury.TokenSymbol.ShouldBe(ProfitContractTestConsts.NativeTokenSymbol);
            treasury.TotalAmount.ShouldBe((long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2));

            var treasuryAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = TreasuryHash
                });
            var treasuryBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = treasuryAddress
            })).Balance;

            treasuryBalance.ShouldBe((long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2));
        }

        [Fact]
        public async Task ProfitContract_CreateProfitItem()
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            await creator.CreateProfitItem.SendAsync(new CreateProfitItemInput
            {
                TokenSymbol = ProfitContractTestConsts.NativeTokenSymbol,
            });

            var createdProfitIds = (await creator.GetCreatedProfitItems.CallAsync(new GetCreatedProfitItemsInput
            {
                Creator = creatorAddress
            })).ProfitIds;

            createdProfitIds.Count.ShouldBe(1);

            var profitId = createdProfitIds.First();
            var profitItem = await creator.GetProfitItem.CallAsync(profitId);

            profitItem.Creator.ShouldBe(creatorAddress);
            profitItem.TokenSymbol.ShouldBe(ProfitContractTestConsts.NativeTokenSymbol);
            profitItem.CurrentPeriod.ShouldBe(1);
            profitItem.ExpiredPeriodNumber.ShouldBe(ProfitContractConsts.DefaultExpiredPeriodNumber);
            profitItem.TotalWeight.ShouldBe(0);
            profitItem.TotalAmount.ShouldBe(0);

            var itemBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = profitItem.VirtualAddress
            })).Balance;

            Assert.Equal(0, itemBalance);
        }

        /// <summary>
        /// Of course it's okay for an address to creator many profit items.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_CreateManyProfitItems()
        {
            const int createTimes = 5;

            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            for (var i = 0; i < createTimes; i++)
            {
                var executionResult = await creator.CreateProfitItem.SendAsync(new CreateProfitItemInput
                {
                    TokenSymbol = ProfitContractTestConsts.NativeTokenSymbol,
                });
                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var createdProfitIds = await creator.GetCreatedProfitItems.CallAsync(new GetCreatedProfitItemsInput
            {
                Creator = creatorAddress
            });

            createdProfitIds.ProfitIds.Count.ShouldBe(createTimes);
        }

        [Fact]
        public async Task ProfitContract_CreateProfitItemWithInvalidTokenSymbol()
        {
            var creator = Creators[0];

            var executionResult = await creator.CreateProfitItem.SendAsync(new CreateProfitItemInput
            {
                TokenSymbol = "WTF"
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Invalid token symbol.");
        }

        [Fact]
        public async Task ProfitContract_AddProfits()
        {
            const int amount = 1000;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();

            // Add profits to virtual address of this profit item.
            await creator.AddProfits.SendAsync(new AddProfitsInput
            {
                ProfitId = profitId,
                Amount = amount
            });

            // Check profit item and corresponding balance.
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                Assert.Equal(amount, profitItem.TotalAmount);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = profitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount);
            }

            // Add profits to release profits virtual address of this profit item.
            const int period = 3;
            await creator.AddProfits.SendAsync(new AddProfitsInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = period
            });

            // Check profit item and corresponding balance.
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                // Total amount stay.
                profitItem.TotalAmount.ShouldBe(amount);

                var virtualAddress = await creator.GetProfitItemVirtualAddress.CallAsync(
                    new GetProfitItemVirtualAddressInput
                    {
                        ProfitId = profitId,
                        Period = period
                    });
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount);

                var releasedProfitInformation = await creator.GetReleasedProfitsInformation.CallAsync(
                    new GetReleasedProfitsInformationInput
                    {
                        ProfitId = profitId,
                        Period = period
                    });
                releasedProfitInformation.IsReleased.ShouldBe(false);
                releasedProfitInformation.TotalWeight.ShouldBe(0);
                releasedProfitInformation.ProfitsAmount.ShouldBe(amount);
            }
        }

        /// <summary>
        /// It's valid for a third party account to add profits to any profit item.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_AddProfits_ByThirdParty()
        {
            const long amountReleasedByCreator = 1000;
            const long amountAddedByGoodGuy = 1000;
            const long weight = 100;

            var creator = Creators[0];
            var goodGuy = Creators[1];

            var profitId = await CreateProfitItem();

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                ProfitId = profitId,
                Receiver = SampleAddress.AddressList[0],
                Weight = weight
            });

            // Add profits to virtual address of this profit item.
            await goodGuy.AddProfits.SendAsync(new AddProfitsInput
            {
                ProfitId = profitId,
                Amount = amountAddedByGoodGuy,
            });

            // Check profit item.
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalAmount.ShouldBe(amountReleasedByCreator);
            }
            
            // Add profits to release profits virtual address of this profit item.
            await goodGuy.AddProfits.SendAsync(new AddProfitsInput
            {
                ProfitId = profitId,
                Amount = amountAddedByGoodGuy,
                Period = 1
            });

            // Check balance of period 1
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                // Total amount stay.
                profitItem.TotalAmount.ShouldBe(amountReleasedByCreator);

                var releasedProfitsInformation = await creator.GetReleasedProfitsInformation.CallAsync(
                    new GetReleasedProfitsInformationInput
                    {
                        ProfitId = profitId,
                        Period = 1
                    });
                releasedProfitsInformation.ProfitsAmount.ShouldBe(amountReleasedByCreator);
                // total_weight is 0 before releasing.
                releasedProfitsInformation.TotalWeight.ShouldBe(0);
                releasedProfitsInformation.IsReleased.ShouldBe(false);

                var virtualAddress = await creator.GetProfitItemVirtualAddress.CallAsync(
                    new GetProfitItemVirtualAddressInput
                    {
                        ProfitId = profitId,
                        Period = 1
                    });
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amountReleasedByCreator);
            }

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Period = 1,
                Amount = amountReleasedByCreator
            });

            // Creator can release profits of this period.
            {
                var releasedProfitsInformation = await creator.GetReleasedProfitsInformation.CallAsync(
                    new GetReleasedProfitsInformationInput
                    {
                        ProfitId = profitId,
                        Period = 1
                    });
                releasedProfitsInformation.ProfitsAmount.ShouldBe(amountReleasedByCreator + amountAddedByGoodGuy);
                releasedProfitsInformation.TotalWeight.ShouldBe(weight);
                releasedProfitsInformation.IsReleased.ShouldBe(true);
            }
        }

        [Fact]
        public async Task ProfitContract_RegisterSubProfitItem()
        {
            const int weight1 = 80;
            const int weight2 = 20;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();
            var subProfitId1 = await CreateProfitItem(1);
            var subProfitId2 = await CreateProfitItem(2);

            var subProfitItem1 = await creator.GetProfitItem.CallAsync(subProfitId1);
            var subProfitItem2 = await creator.GetProfitItem.CallAsync(subProfitId2);

            await creator.RegisterSubProfitItem.SendAsync(new RegisterSubProfitItemInput
            {
                ProfitId = profitId,
                SubProfitId = subProfitId1,
                SubItemWeight = weight1
            });

            var profitDetails1 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                ProfitId = profitId,
                Receiver = subProfitItem1.VirtualAddress
            });

            // Check the total_weight of profit item.
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalWeight.ShouldBe(weight1);
            }

            profitDetails1.Details.Count.ShouldBe(1);
            profitDetails1.Details.First().StartPeriod.ShouldBe(1);
            profitDetails1.Details.First().EndPeriod.ShouldBe(long.MaxValue);
            profitDetails1.Details.First().LastProfitPeriod.ShouldBe(0);
            profitDetails1.Details.First().Weight.ShouldBe(weight1);

            await creator.RegisterSubProfitItem.SendAsync(new RegisterSubProfitItemInput
            {
                ProfitId = profitId,
                SubProfitId = subProfitId2,
                SubItemWeight = weight2
            });

            var profitDetails2 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                ProfitId = profitId,
                Receiver = subProfitItem2.VirtualAddress
            });

            // Check the total_weight of profit item.
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalWeight.ShouldBe(weight1 + weight2);
            }

            profitDetails2.Details.Count.ShouldBe(1);
            profitDetails2.Details.First().StartPeriod.ShouldBe(1);
            profitDetails2.Details.First().EndPeriod.ShouldBe(long.MaxValue);
            profitDetails2.Details.First().LastProfitPeriod.ShouldBe(0);
            profitDetails2.Details.First().Weight.ShouldBe(weight2);
        }

        [Fact]
        public async Task ProfitContract_AddWeight()
        {
            var creator = Creators[0];

            var profitId = await CreateProfitItem();

            const int weight1 = 100;
            const int weight2 = 200;
            var receiver1 = SampleAddress.AddressList[0];
            var receiver2 = SampleAddress.AddressList[1];

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiver1,
                ProfitId = profitId,
                Weight = weight1
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalWeight.ShouldBe(weight1);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiver1
                });
                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Weight.ShouldBe(weight1);
                profitDetails.Details[0].EndPeriod.ShouldBe(long.MaxValue);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            const int endPeriod = 100;
            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiver2,
                ProfitId = profitId,
                Weight = weight2,
                EndPeriod = endPeriod
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalWeight.ShouldBe(weight1 + weight2);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiver2
                });
                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Weight.ShouldBe(weight2);
                profitDetails.Details[0].EndPeriod.ShouldBe(endPeriod);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_AddWeight_IncorrectEndPeriod()
        {
            const long amount = 100;
            const long weight = 10;

            var creator = Creators[0];
            var receiver = SampleAddress.AddressList[0];

            var profitId = await CreateProfitItem();

            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.CurrentPeriod.ShouldBe(1);
            }

            await TransferToProfitItemVirtualAddress(profitId);

            // Current period: 1
            {
                var executionResult = await creator.AddWeight.SendAsync(new AddWeightInput
                {
                    ProfitId = profitId,
                    Receiver = receiver,
                    Weight = weight,
                    EndPeriod = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // We need to add weight successfully for further testing.
            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                ProfitId = profitId,
                Receiver = receiver,
                Weight = weight
            });

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.CurrentPeriod.ShouldBe(2);
            }

            // Current period: 2
            {
                var executionResult = await creator.AddWeight.SendAsync(new AddWeightInput
                {
                    ProfitId = profitId,
                    Receiver = receiver,
                    Weight = weight,
                    EndPeriod = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                executionResult.TransactionResult.Error.ShouldContain("Invalid end period.");
            }
        }

        [Fact]
        public async Task ProfitContract_AddWeight_ProfitItemNotFound()
        {
            var creator = Creators[0];

            var executionResult = await creator.AddWeight.SendAsync(new AddWeightInput
            {
                ProfitId = Hash.FromString("hash"),
                Receiver = SampleAddress.AddressList[0],
                Weight = 100
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        /// <summary>
        /// Every time adding weight to a receiver, will remove expired and used up profit details.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_AddWeight_RemoveExpiredProfitDetails()
        {
            const long expiredPeriodNumber = 1;
            const long amount = 150;
            const long weight = 10;

            var creator = Creators[0];
            var receiver = Creators[1];
            
            var receiverAddress = Address.FromPublicKey(CreatorMinerKeyPair[1].PublicKey);

            var profitId = await CreateProfitItem(0, expiredPeriodNumber);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                ProfitId = profitId,
                Receiver = receiverAddress,
                Weight = weight,
                EndPeriod = 1
            });

            await TransferToProfitItemVirtualAddress(profitId, amount);

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount / 3,
                Period = 1
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Weight.ShouldBe(weight);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].EndPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount / 3,
                Period = 2
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Weight.ShouldBe(weight);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].EndPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount / 3,
                Period = 3
            });

            await receiver.Profit.SendAsync(new ProfitInput
            {
                ProfitId = profitId
            });
            
            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                ProfitId = profitId,
                Receiver = receiverAddress,
                Weight = weight * 2,
                EndPeriod = 4
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Weight.ShouldBe(weight * 2);
                profitDetails.Details[0].StartPeriod.ShouldBe(4);
                profitDetails.Details[0].EndPeriod.ShouldBe(4);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_SubWeight()
        {
            const int weight = 100;
            const int amount = 100;

            var creator = Creators[0];
            var receiver = Normal[0];
            var receiverAddress = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress,
                ProfitId = profitId,
                Weight = weight,
                EndPeriod = 1
            });

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });
            
            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalWeight.ShouldBe(weight);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });
                profitDetails.Details.Count.ShouldBe(1);
            }

            await receiver.Profit.SendAsync(new ProfitInput {ProfitId = profitId});
            
            await creator.SubWeight.SendAsync(new SubWeightInput
            {
                Receiver = receiverAddress,
                ProfitId = profitId
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                profitItem.TotalWeight.ShouldBe(0);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });
                profitDetails.Details.Count.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_SubWeight_ProfitItemNotFound()
        {
            var creator = Creators[0];

            var executionResult = await creator.SubWeight.SendAsync(new SubWeightInput
            {
                ProfitId = Hash.FromString("hash2"),
                Receiver = SampleAddress.AddressList[0]
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_WithoutEnoughBalance()
        {
            const long amount = 100;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = SampleAddress.AddressList[0],
                ProfitId = profitId,
                Weight = 100
            });

            var executionResult = await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Insufficient profits amount.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_InvalidPeriod()
        {
            const long amount = 100;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = SampleAddress.AddressList[0],
                ProfitId = profitId,
                Weight = 100
            });

            var executionResult = await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 2
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Invalid period.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_NotCreator()
        {
            const long amount = 100;

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId);

            // The actual creator is Creators[0]
            var anotherGuy = Creators[1];

            var executionResult = await anotherGuy.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Only creator can release profits.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_ProfitItemNotFound()
        {
            const long amount = 100;

            var user = Creators[0];

            var executionResult = await user.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = Hash.FromString("hash3"),
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits()
        {
            const long amount = 100;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId, amount * 2);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = SampleAddress.AddressList[0],
                ProfitId = profitId,
                Weight = 100
            });

            // First time.
            {
                var executionResult = await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
                {
                    ProfitId = profitId,
                    Amount = amount,
                    Period = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Second time.
            {
                var executionResult = await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
                {
                    ProfitId = profitId,
                    Amount = amount,
                    Period = 2
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_BurnProfits()
        {
            const long amount = 100;
            // Will burn specific amount of profits. Because no one can receive profits from -1 period.
            const long period = -1;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId, amount);
            
            var executionResult = await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = period
            });
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            // Check balance.
            var address = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(new GetProfitItemVirtualAddressInput
                {ProfitId = profitId, Period = -1});
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {Owner = address, Symbol = ProfitContractTestConsts.NativeTokenSymbol});
            balance.Balance.ShouldBe(amount);
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_WithSubProfitItems()
        {
            const long amount = 100;
            const long weight1 = 80;
            const long weight2 = 20;

            var creator = Creators[0];

            var profitId = await CreateProfitItem();
            var subProfitId1 = await CreateProfitItem(1);
            var subProfitId2 = await CreateProfitItem(2);

            await TransferToProfitItemVirtualAddress(profitId);

            // Check balance of main profit item.
            {
                var profitItem = await creator.GetProfitItem.CallAsync(profitId);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = profitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount);
            }

            await creator.RegisterSubProfitItem.SendAsync(new RegisterSubProfitItemInput
            {
                ProfitId = profitId,
                SubProfitId = subProfitId1,
                SubItemWeight = weight1
            });

            await creator.RegisterSubProfitItem.SendAsync(new RegisterSubProfitItemInput
            {
                ProfitId = profitId,
                SubProfitId = subProfitId2,
                SubItemWeight = weight2
            });

            var executionResult = await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance of first sub profit item.
            {
                var subProfitItem = await creator.GetProfitItem.CallAsync(subProfitId1);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = subProfitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount.Mul(weight1).Div(weight1 + weight2));
            }

            // Check balance of second sub profit item.
            {
                var subProfitItem = await creator.GetProfitItem.CallAsync(subProfitId2);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = subProfitItem.VirtualAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount.Mul(weight2).Div(weight1 + weight2));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit()
        {
            const long weight = 100;
            const long amount = 100;

            var creator = Creators[0];
            var receiver = Normal[0];
            var receiverAddress = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress,
                ProfitId = profitId,
                Weight = weight
            });

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            await receiver.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = receiverAddress,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount);
        }
        
        [Fact]
        public async Task ProfitContract_Profit_TwoReceivers()
        {
            const long weight1 = 100;
            const long weight2 = 400;
            const long amount = 100;

            var creator = Creators[0];
            var receiver1 = Normal[0];
            var receiver2 = Normal[1];
            var receiverAddress1 = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);
            var receiverAddress2 = Address.FromPublicKey(NormalMinerKeyPair[1].PublicKey);

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress1,
                ProfitId = profitId,
                Weight = weight1
            });
            
            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress2,
                ProfitId = profitId,
                Weight = weight2
            });

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            await receiver1.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            // Check balance of receiver 1.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress1,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight1.Mul(amount).Div(weight1 + weight2));
            }
            
            await receiver2.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            // Check balance of receiver 2.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress2,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight2.Mul(amount).Div(weight1 + weight2));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_RegisteredSubProfitItems()
        {
            const long weight1 = 100;
            const long weight2 = 400;
            const long weight3 = 500;
            const long amount = 100;

            var creator = Creators[0];
            var receiver1 = Normal[0];
            var receiver2 = Normal[1];
            var receiverAddress1 = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);
            var receiverAddress2 = Address.FromPublicKey(NormalMinerKeyPair[1].PublicKey);

            var profitId = await CreateProfitItem();
            var subProfitId = await CreateProfitItem(1);

            await TransferToProfitItemVirtualAddress(profitId);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress1,
                ProfitId = profitId,
                Weight = weight1
            });
            
            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress2,
                ProfitId = profitId,
                Weight = weight2
            });

            await creator.RegisterSubProfitItem.SendAsync(new RegisterSubProfitItemInput
            {
                ProfitId = profitId,
                SubProfitId = subProfitId,
                SubItemWeight = weight3
            });

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });

            await receiver1.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            // Check balance of receiver 1.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress1,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight1.Mul(amount).Div(weight1.Add(weight2).Add(weight3)));
            }
            
            await receiver2.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            // Check balance of receiver 2.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress2,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight2.Mul(amount).Div(weight1.Add(weight2).Add(weight3)));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_ProfitItemNotFound()
        {
            var receiver = Normal[0];

            var executionResult = await receiver.Profit.SendAsync(new ProfitInput
            {
                ProfitId = Hash.FromString("hash4")
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit item not found.");
        }

        [Fact]
        public async Task ProfitContract_Profit_NotRegisteredBefore()
        {
            const long amount = 100;

            var creator = Creators[0];
            var receiver = Normal[0];

            var profitId = await CreateProfitItem();
            
            await TransferToProfitItemVirtualAddress(profitId);

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                ProfitId = profitId,
                Amount = amount,
                Period = 1
            });
            
            var executionResult = await receiver.Profit.SendAsync(new ProfitInput
            {
                ProfitId = profitId
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit details not found.");
        }

        [Fact]
        public async Task ProfitContract_Profit_MultiplePeriods()
        {
            const int periodCount = 5;
            const long weight = 100;
            const long amount = 100;

            var creator = Creators[0];
            var receiver = Normal[0];
            var receiverAddress = Address.FromPublicKey(NormalMinerKeyPair[0].PublicKey);

            var profitId = await CreateProfitItem();

            await TransferToProfitItemVirtualAddress(profitId, amount * periodCount + amount);

            await creator.AddWeight.SendAsync(new AddWeightInput
            {
                Receiver = receiverAddress,
                ProfitId = profitId,
                Weight = weight
            });

            for (var i = 0; i < periodCount; i++)
            {
                await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
                {
                    ProfitId = profitId,
                    Amount = amount,
                    Period = i + 1
                });
            }

            await receiver.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount * periodCount);

                var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });
                details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 1);
            }

            await creator.ReleaseProfit.SendAsync(new ReleaseProfitInput
            {
                Period = periodCount + 1,
                Amount = amount,
                ProfitId = profitId
            });

            await receiver.Profit.SendAsync(new ProfitInput {ProfitId = profitId});

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = receiverAddress,
                    Symbol = ProfitContractTestConsts.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount * periodCount + amount);

                var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = profitId,
                    Receiver = receiverAddress
                });
                details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 2);
            }          
        }

        private async Task<Hash> CreateProfitItem(int returnIndex = 0,
            long expiredPeriodNumber = ProfitContractConsts.DefaultExpiredPeriodNumber)
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            await creator.CreateProfitItem.SendAsync(new CreateProfitItemInput
            {
                TokenSymbol = ProfitContractTestConsts.NativeTokenSymbol,
                ExpiredPeriodNumber = expiredPeriodNumber
            });

            var createdProfitIds = (await creator.GetCreatedProfitItems.CallAsync(new GetCreatedProfitItemsInput
            {
                Creator = creatorAddress
            })).ProfitIds;

            return createdProfitIds[returnIndex];
        }

        private async Task TransferToProfitItemVirtualAddress(Hash profitId, long amount = 100)
        {
            await ProfitContractStub.AddProfits.SendAsync(new AddProfitsInput
            {
                ProfitId = profitId,
                Amount = amount
            });
        }
    }
}