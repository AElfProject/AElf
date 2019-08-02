using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit
{
    public partial class ProfitTests : ProfitContractTestBase
    {
        private const long ClaimTransactionFee = 1_00000000;

        public ProfitTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ProfitContract_CheckTreasury_Test()
        {
            await CreateTreasury();
        }

        /// <summary>
        /// Of course it's okay for an address to creator many profit items.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_CreateManyProfitItems_Test()
        {
            const int createTimes = 5;

            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorKeyPair[0].PublicKey);

            for (var i = 0; i < createTimes; i++)
            {
                var executionResult = await creator.CreateScheme.SendAsync(new CreateSchemeInput
                {
                });
                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var createdSchemeIds = await creator.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
            {
                Manager = creatorAddress
            });

            createdSchemeIds.SchemeIds.Count.ShouldBe(createTimes);
        }

        [Fact]
        public async Task ProfitContract_DistributeProfits_Test()
        {
            const int amount = 1000;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            // Add profits to virtual address of this profit item.
            await creator.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
            });

            // Check profit item and corresponding balance.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                Assert.Equal(amount, profitItem.UndistributedProfits[ProfitContractTestConstants.NativeTokenSymbol]);

                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = profitItem.VirtualAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount);
            }

            // Add profits to release profits virtual address of this profit item.
            const int period = 3;
            await creator.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Period = period,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
            });

            // Check profit item and corresponding balance.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                // Total amount stay.
                profitItem.UndistributedProfits[ProfitContractTestConstants.NativeTokenSymbol].ShouldBe(amount);

                var virtualAddress = await creator.GetSchemeAddress.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = period
                    });
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount);

                var releasedProfitInformation = await creator.GetDistributedProfitsInfo.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = period
                    });
                releasedProfitInformation.IsReleased.ShouldBe(false);
                releasedProfitInformation.TotalShares.ShouldBe(0);
                releasedProfitInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol].ShouldBe(amount);
            }
        }

        /// <summary>
        /// It's valid for a third party account to add profits to any profit item.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_ContributeProfits_ByThirdParty_Test()
        {
            const long amountReleasedByCreator = 1000;
            const long amountAddedByGoodGuy = 1000;
            const long shares = 100;

            var creator = Creators[0];
            var goodGuy = Creators[1];

            var schemeId = await CreateScheme();

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput()
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = SampleAddress.AddressList[0], Shares = shares}
            });

            // Add profits to virtual address of this profit item.
            await goodGuy.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amountAddedByGoodGuy,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
            });

            // Check profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.UndistributedProfits[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(amountReleasedByCreator);
            }

            // Add profits to release profits virtual address of this profit item.
            await goodGuy.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amountAddedByGoodGuy,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            // Check balance of period 1
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                // Total amount stay.
                profitItem.UndistributedProfits[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(amountReleasedByCreator);

                var releasedProfitsInformation = await creator.GetDistributedProfitsInfo.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = 1
                    });
                releasedProfitsInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(amountReleasedByCreator);
                // total_Shares is 0 before releasing.
                releasedProfitsInformation.TotalShares.ShouldBe(0);
                releasedProfitsInformation.IsReleased.ShouldBe(false);

                var virtualAddress = await creator.GetSchemeAddress.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = 1
                    });
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amountReleasedByCreator);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Period = 1,
                Amount = amountReleasedByCreator,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            // Creator can release profits of this period.
            {
                var releasedProfitsInformation = await creator.GetDistributedProfitsInfo.CallAsync(
                    new SchemePeriod
                    {
                        SchemeId = schemeId,
                        Period = 1
                    });
                releasedProfitsInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(amountReleasedByCreator + amountAddedByGoodGuy);
                releasedProfitsInformation.TotalShares.ShouldBe(shares);
                releasedProfitsInformation.IsReleased.ShouldBe(true);
            }
        }

        [Fact]
        public async Task ProfitContract_RemoveSubScheme_Test()
        {
            const int shares1 = 80;
            const int shares2 = 20;

            var creator = Creators[0];

            var schemeId = await CreateScheme();
            var subSchemeId1 = await CreateScheme(1);
            var subSchemeId2 = await CreateScheme(2);

            var subProfitItem1 = await creator.GetScheme.CallAsync(subSchemeId1);
            var subProfitItem2 = await creator.GetScheme.CallAsync(subSchemeId2);

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId1,
                SubSchemeShares = shares1
            });

            var profitDetails1 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = subProfitItem1.VirtualAddress
            });

            // Check the total_weight of profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1);
            }

            profitDetails1.Details.Count.ShouldBe(1);
            profitDetails1.Details.First().StartPeriod.ShouldBe(1);
            profitDetails1.Details.First().EndPeriod.ShouldBe(long.MaxValue);
            profitDetails1.Details.First().LastProfitPeriod.ShouldBe(0);
            profitDetails1.Details.First().Shares.ShouldBe(shares1);

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput()
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId2,
                SubSchemeShares = shares2
            });

            var profitDetails2 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = subProfitItem2.VirtualAddress
            });

            // Check the total_weight of profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1 + shares2);
            }

            profitDetails2.Details.Count.ShouldBe(1);
            profitDetails2.Details.First().StartPeriod.ShouldBe(1);
            profitDetails2.Details.First().EndPeriod.ShouldBe(long.MaxValue);
            profitDetails2.Details.First().LastProfitPeriod.ShouldBe(0);
            profitDetails2.Details.First().Shares.ShouldBe(shares2);

            //remove sub scheme1
            {
                var removeSubSchemeResult = await creator.RemoveSubScheme.SendAsync(new RemoveSubSchemeInput
                {
                    SchemeId = schemeId,
                    SubSchemeId = subSchemeId1,
                    SubItemCreator = Address.FromPublicKey(CreatorKeyPair[0].PublicKey)
                });
                removeSubSchemeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares2);
                profitItem.SubSchemes.Count.ShouldBe(1);
                profitItem.SubSchemes.First().Shares.ShouldBe(shares2);
            }
        }

        [Fact]
        public async Task ProfitContract_AddWeight_Test()
        {
            var creator = Creators[0];

            var schemeId = await CreateScheme();

            const int shares1 = 100;
            const int shares2 = 200;
            var receiver1 = SampleAddress.AddressList[0];
            var receiver2 = SampleAddress.AddressList[1];

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = receiver1, Shares = shares1},
                SchemeId = schemeId,
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiver1
                });
                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares1);
                profitDetails.Details[0].EndPeriod.ShouldBe(long.MaxValue);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            const int endPeriod = 100;
            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = receiver2, Shares = shares2},
                SchemeId = schemeId,
                EndPeriod = endPeriod
            });

            // Check total_shares and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares1 + shares2);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiver2
                });
                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares2);
                profitDetails.Details[0].EndPeriod.ShouldBe(endPeriod);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_AddBeneficiary_IncorrectEndPeriod_Test()
        {
            const long amount = 100;
            const long shares = 10;

            var creator = Creators[0];
            var beneficiary = SampleAddress.AddressList[0];

            var schemeId = await CreateScheme();

            {
                var scheme = await creator.GetScheme.CallAsync(schemeId);
                scheme.CurrentPeriod.ShouldBe(1);
            }

            await TransferToProfitItemVirtualAddress(schemeId);

            // Current period: 1
            {
                var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
                {
                    SchemeId = schemeId,
                    BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiary, Shares = shares},
                    EndPeriod = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // We need to add Shares successfully for further testing.
            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiary, Shares = shares},
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            {
                var scheme = await creator.GetScheme.CallAsync(schemeId);
                scheme.CurrentPeriod.ShouldBe(2);
            }

            // Current period: 2
            {
                var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
                {
                    SchemeId = schemeId,
                    BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiary, Shares = shares},
                    EndPeriod = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                executionResult.TransactionResult.Error.ShouldContain("Invalid end period.");
            }
        }

        [Fact]
        public async Task ProfitContract_AddWeight_ProfitItemNotFound_Test()
        {
            var creator = Creators[0];

            var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = Hash.FromString("SchemeId"),
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = SampleAddress.AddressList[0], Shares = 100},
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Scheme not found.");
        }

        /// <summary>
        /// Every time adding Shares to a Beneficiary, will remove expired and used up profit details.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProfitContract_AddWeight_RemoveExpiredProfitDetails_Test()
        {
            const long expiredPeriodNumber = 1;
            const long amount = 150;
            const long shares = 10;

            var creator = Creators[0];
            var beneficiary = Creators[1];

            var receiverAddress = Address.FromPublicKey(CreatorKeyPair[1].PublicKey);

            var schemeId = await CreateScheme(0, expiredPeriodNumber);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = receiverAddress, Shares = shares},
                EndPeriod = 1
            });

            await TransferToProfitItemVirtualAddress(schemeId, amount);

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount / 3,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].EndPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount / 3,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 2
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares);
                profitDetails.Details[0].StartPeriod.ShouldBe(1);
                profitDetails.Details[0].EndPeriod.ShouldBe(1);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount / 3,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 3
            });

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = receiverAddress, Shares = shares * 2},
                EndPeriod = 4
            });

            // Check details
            {
                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });

                profitDetails.Details.Count.ShouldBe(1);
                profitDetails.Details[0].Shares.ShouldBe(shares * 2);
                profitDetails.Details[0].StartPeriod.ShouldBe(4);
                profitDetails.Details[0].EndPeriod.ShouldBe(4);
                profitDetails.Details[0].LastProfitPeriod.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_RemoveBeneficiary_Test()
        {
            const int shares = 100;
            const int amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];
            var receiverAddress = Address.FromPublicKey(NormalKeyPair[0].PublicKey);

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = receiverAddress, Shares = shares},
                SchemeId = schemeId,
                EndPeriod = 1
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(shares);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });
                profitDetails.Details.Count.ShouldBe(1);
            }

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            await creator.RemoveBeneficiary.SendAsync(new RemoveBeneficiaryInput
            {
                Beneficiary = receiverAddress,
                SchemeId = schemeId
            });

            // Check total_weight and profit_detail
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                profitItem.TotalShares.ShouldBe(0);

                var profitDetails = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = receiverAddress
                });
                profitDetails.Details.Count.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ProfitContract_RemoveBeneficiary_SchemeNotFound_Test()
        {
            var creator = Creators[0];

            var executionResult = await creator.RemoveBeneficiary.SendAsync(new RemoveBeneficiaryInput
            {
                SchemeId = Hash.FromString("SchemeId"),
                Beneficiary = SampleAddress.AddressList[0]
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Scheme not found.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_WithoutEnoughBalance_Test()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = SampleAddress.AddressList[0], Shares = 100},
                SchemeId = schemeId,
            });

            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Insufficient balance.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_InvalidPeriod_Test()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = SampleAddress.AddressList[0], Shares = 100},
                SchemeId = schemeId,
            });

            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 2
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Invalid period.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_NotCreator_Test()
        {
            const long amount = 100;

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            // The actual creator is Creators[0]
            var anotherGuy = Creators[1];

            var executionResult = await anotherGuy.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Only manager");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_ProfitItemNotFound_Test()
        {
            const long amount = 100;

            var user = Creators[0];

            var executionResult = await user.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = Hash.FromString("SchemeId"),
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Scheme not found.");
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_Test()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId, amount * 2);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = SampleAddress.AddressList[0], Shares = 100},
                SchemeId = schemeId,
            });

            // First time.
            {
                var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Period = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Second time.
            {
                var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Period = 2
                });
                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task ProfitContract_ReleaseProfits_WithSubProfitItems_Test()
        {
            const long amount = 100;
            const long weight1 = 80;
            const long weight2 = 20;

            var creator = Creators[0];

            var schemeId = await CreateScheme();
            var subSchemeId1 = await CreateScheme(1);
            var subSchemeId2 = await CreateScheme(2);

            await TransferToProfitItemVirtualAddress(schemeId);

            // Check balance of main profit item.
            {
                var profitItem = await creator.GetScheme.CallAsync(schemeId);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = profitItem.VirtualAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount);
            }

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId1,
                SubSchemeShares = weight1
            });

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId2,
                SubSchemeShares = weight2
            });

            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance of first sub profit item.
            {
                var subProfitItem = await creator.GetScheme.CallAsync(subSchemeId1);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = subProfitItem.VirtualAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount.Mul(weight1).Div(weight1 + weight2));
            }

            // Check balance of second sub profit item.
            {
                var subProfitItem = await creator.GetScheme.CallAsync(subSchemeId2);
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = subProfitItem.VirtualAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;

                balance.ShouldBe(amount.Mul(weight2).Div(weight1 + weight2));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_Test()
        {
            const long shares = 100;
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];
            var beneficiaryAddress = Address.FromPublicKey(NormalKeyPair[0].PublicKey);
            var initialBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiaryAddress, Shares = shares},
                SchemeId = schemeId,
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount + initialBalance - ClaimTransactionFee);
        }

        [Fact]
        public async Task ProfitContract_Profit_TwoReceivers_Test()
        {
            const long weight1 = 100;
            const long weight2 = 400;
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary1 = Normal[0];
            var beneficiary2 = Normal[1];
            var beneficiaryAddress1 = Address.FromPublicKey(NormalKeyPair[0].PublicKey);
            var beneficiaryAddress2 = Address.FromPublicKey(NormalKeyPair[1].PublicKey);

            var initialBalance1 = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress1,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            var initialBalance2 = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress2,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiaryAddress1, Shares = weight1},
                SchemeId = schemeId,
            });

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiaryAddress2, Shares = weight2},
                SchemeId = schemeId,
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            await beneficiary1.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            // Check balance of Beneficiary 1.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress1,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight1.Mul(amount).Div(weight1 + weight2).Add(initialBalance1)
                    .Sub(ClaimTransactionFee));
            }

            await beneficiary2.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            // Check balance of Beneficiary 2.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress2,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight2.Mul(amount).Div(weight1 + weight2).Add(initialBalance2)
                    .Sub(ClaimTransactionFee));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_RegisteredSubProfitItems_Test()
        {
            const long weight1 = 100;
            const long weight2 = 400;
            const long weight3 = 500;
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary1 = Normal[0];
            var beneficiary2 = Normal[1];
            var beneficiaryAddress1 = Address.FromPublicKey(NormalKeyPair[0].PublicKey);
            var beneficiaryAddress2 = Address.FromPublicKey(NormalKeyPair[1].PublicKey);

            var initialBalance1 = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress1,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            var initialBalance2 = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress2,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;

            var schemeId = await CreateScheme();
            var subSchemeId = await CreateScheme(1);

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiaryAddress1, Shares = weight1},
                SchemeId = schemeId,
            });

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiaryAddress2, Shares = weight2},
                SchemeId = schemeId,
            });

            await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId,
                SubSchemeShares = weight3
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            await beneficiary1.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            // Check balance of Beneficiary 1.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress1,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight1.Mul(amount).Div(weight1.Add(weight2).Add(weight3)).Add(initialBalance1)
                    .Sub(ClaimTransactionFee));
            }

            await beneficiary2.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            // Check balance of Beneficiary 2.
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress2,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(weight2.Mul(amount).Div(weight1.Add(weight2).Add(weight3)).Add(initialBalance2)
                    .Sub(ClaimTransactionFee));
            }
        }

        [Fact]
        public async Task ProfitContract_Profit_ProfitItemNotFound_Test()
        {
            var beneficiary = Normal[0];

            var executionResult = await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = Hash.FromString("SchemeId"),
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Scheme not found.");
        }

        [Fact]
        public async Task ProfitContract_Profit_NotRegisteredBefore_Test()
        {
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId);

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Period = 1
            });

            var executionResult = await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.ShouldContain("Profit details not found.");
        }

        [Fact]
        public async Task ProfitContract_Profit_MultiplePeriods_Test()
        {
            const int periodCount = 5;
            const long shares = 100;
            const long amount = 100;

            var creator = Creators[0];
            var beneficiary = Normal[0];
            var beneficiaryAddress = Address.FromPublicKey(NormalKeyPair[0].PublicKey);
            var initialBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId, amount * periodCount + amount);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = beneficiaryAddress, Shares = shares},
            });

            for (var i = 0; i < periodCount; i++)
            {
                await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Period = i + 1
                });
            }

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount * periodCount + initialBalance - ClaimTransactionFee);

                var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = beneficiaryAddress
                });
                details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 1);
            }

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                Period = periodCount + 1,
                Amount = amount,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                SchemeId = schemeId
            });

            await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = beneficiaryAddress,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol
                })).Balance;
                balance.ShouldBe(amount * periodCount + amount + initialBalance - ClaimTransactionFee * 2);

                var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = schemeId,
                    Beneficiary = beneficiaryAddress
                });
                details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 2);
            }
        }

        private async Task<Hash> CreateScheme(int returnIndex = 0,
            long profitReceivingDuePeriodCount = ProfitContractConstants.DefaultProfitReceivingDuePeriodCount)
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorKeyPair[0].PublicKey);

            await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = profitReceivingDuePeriodCount
            });

            var createdSchemeIds = (await creator.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
            {
                Manager = creatorAddress
            })).SchemeIds;

            return createdSchemeIds[returnIndex];
        }

        [Fact]
        public async Task ContributeProfits_MultipleTimes_Test()
        {
            const long amount = 100;

            var creator = Creators[0];

            var schemeId = await CreateScheme();

            await TransferToProfitItemVirtualAddress(schemeId, amount * 2);

            await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = SampleAddress.AddressList[0], Shares = 100},
                SchemeId = schemeId,
            });

            // First time.
            {
                var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
                {
                    SchemeId = schemeId,
                    Amount = amount,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Period = 1
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                var transactionResult = await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Amount = 100,
                    Period = 1
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.ShouldContain("already released");
            }
            
            //Second time
            {
                var transactionResult = await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Amount = 100,
                    Period = 2
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                transactionResult = await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Amount = 100,
                    Period = 2
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private async Task TransferToProfitItemVirtualAddress(Hash schemeId, long amount = 100)
        {
            await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Amount = amount
            });
        }
    }
}