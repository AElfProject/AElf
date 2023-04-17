using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit;

public partial class ProfitContractTests
{
    /// <summary>
    /// Of course it's okay for an address to creator many profit schemes.
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

        var schemeId = await CreateSchemeAsync();

        // Add profits to virtual address of this profit scheme.
        await creator.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeId = schemeId,
            Amount = amount,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol,
        });

        // Check profit scheme and corresponding balance.
        {
            var profitItem = await creator.GetScheme.CallAsync(schemeId);
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = profitItem.VirtualAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount);
        }

        // Add profits to release profits virtual address of this profit scheme.
        const int period = 3;
        await creator.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeId = schemeId,
            Amount = amount,
            Period = period,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol,
        });

        // Check profit scheme and corresponding balance.
        {
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
            releasedProfitInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol].ShouldBe(amount);
        }
    }

    /// <summary>
    /// It's valid for a third party account to add profits to any profit scheme.
    /// </summary>
    /// <returns></returns>
    [Fact(DisplayName = "A third party contributes profits to a scheme.")]
    public async Task ProfitContract_ContributeProfits_ByThirdParty_Test()
    {
        const long distributingAmount = 1000;
        const long amountAddedByThirdParty = 1000;
        const long shares = 100;

        var creator = Creators[0];
        var thirdParty = Creators[1];

        var schemeId = await CreateSchemeAsync();

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput()
        {
            SchemeId = schemeId,
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = Accounts[0].Address, Shares = shares }
        });

        // Add profits to virtual address of this profit scheme.
        await thirdParty.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeId = schemeId,
            Amount = amountAddedByThirdParty,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol,
        });

        // Add profits to period virtual address of this profit scheme.
        await thirdParty.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeId = schemeId,
            Amount = amountAddedByThirdParty,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol,
            Period = 1
        });

        // Check balance of period 1
        {
            var distributedProfitsInfo = await creator.GetDistributedProfitsInfo.CallAsync(
                new SchemePeriod
                {
                    SchemeId = schemeId,
                    Period = 1
                });
            distributedProfitsInfo.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                .ShouldBe(distributingAmount);
            // total_Shares is 0 before releasing.
            distributedProfitsInfo.TotalShares.ShouldBe(0);
            distributedProfitsInfo.IsReleased.ShouldBe(false);

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
            balance.ShouldBe(distributingAmount);
        }

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            Period = 1,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, distributingAmount }
            }
        });

        // Creator can distribute profits of this period.
        {
            var distributedProfitsInfo = await creator.GetDistributedProfitsInfo.CallAsync(
                new SchemePeriod
                {
                    SchemeId = schemeId,
                    Period = 1
                });
            distributedProfitsInfo.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                .ShouldBe(distributingAmount + amountAddedByThirdParty);
            distributedProfitsInfo.TotalShares.ShouldBe(shares);
            distributedProfitsInfo.IsReleased.ShouldBe(true);
        }
    }

    [Fact]
    public async Task ProfitContract_AddSubScheme_Fail_Test()
    {
        var creator = Creators[0];
        var schemeId = await CreateSchemeAsync();
        var subSchemeId1 = await CreateSchemeAsync(1);

        // scheme id == subScheme Id
        {
            var addSubSchemeRet = await creator.AddSubScheme.SendWithExceptionAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = schemeId,
            });
            addSubSchemeRet.TransactionResult.Error.ShouldContain("Two schemes cannot be same");
        }

        // input.Shares <= 0
        {
            var addSubSchemeRet = await creator.AddSubScheme.SendWithExceptionAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId1,
                SubSchemeShares = 0
            });
            addSubSchemeRet.TransactionResult.Error.ShouldContain("Shares of sub scheme should greater than 0");
        }

        // scheme id or subScheme Id does not exist.
        {
            var addSubSchemeRet = await creator.AddSubScheme.SendWithExceptionAsync(new AddSubSchemeInput
            {
                SchemeId = new Hash(),
                SubSchemeId = subSchemeId1,
                SubSchemeShares = 100
            });
            addSubSchemeRet.TransactionResult.Error.ShouldContain("not found");

            addSubSchemeRet = await creator.AddSubScheme.SendWithExceptionAsync(new AddSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = new Hash(),
                SubSchemeShares = 100
            });
            addSubSchemeRet.TransactionResult.Error.ShouldContain("not found");
        }

        // not the scheme manager
        {
            var creatorWithoutAuthority = Creators[1];
            var addSubSchemeRet = await creatorWithoutAuthority.AddSubScheme.SendWithExceptionAsync(
                new AddSubSchemeInput
                {
                    SchemeId = schemeId,
                    SubSchemeId = subSchemeId1,
                    SubSchemeShares = 100
                });
            addSubSchemeRet.TransactionResult.Error.ShouldContain("Only manager");
        }
    }

    [Fact]
    public async Task ProfitContract_RemoveSubScheme_With_Invalid_Scheme_Id_Test()
    {
        var shares = 100;
        var creator = Creators[0];
        var schemeId = await CreateSchemeAsync();
        var subSchemeId1 = await CreateSchemeAsync(1);
        await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
        {
            SchemeId = schemeId,
            SubSchemeId = subSchemeId1,
            SubSchemeShares = shares
        });

        //schemeId == subSchemeId
        {
            var removeSubSchemeResult = await creator.RemoveSubScheme.SendWithExceptionAsync(
                new RemoveSubSchemeInput
                {
                    SchemeId = schemeId,
                    SubSchemeId = schemeId,
                });
            removeSubSchemeResult.TransactionResult.Error.ShouldContain("Two schemes cannot be same");
        }
    }

    [Fact]
    public async Task ProfitContract_RemoveSubScheme_Without_Authority_Test()
    {
        var shares = 100;
        var schemeId = await CreateSchemeAsync();
        var creatorWithoutAuthority = Creators[1];
        var removeSubSchemeResult = await creatorWithoutAuthority.RemoveSubScheme.SendWithExceptionAsync(
            new RemoveSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = new Hash(),
            });
        removeSubSchemeResult.TransactionResult.Error.ShouldContain("Only manager");
    }

    [Fact]
    public async Task ProfitContract_RemoveSubScheme_Success_Test()
    {
        const int shares1 = 80;
        const int shares2 = 20;

        var creator = Creators[0];

        var schemeId = await CreateSchemeAsync();
        var subSchemeId1 = await CreateSchemeAsync(1);
        var subSchemeId2 = await CreateSchemeAsync(2);

        await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
        {
            SchemeId = schemeId,
            SubSchemeId = subSchemeId1,
            SubSchemeShares = shares1
        });

        await creator.AddSubScheme.SendAsync(new AddSubSchemeInput()
        {
            SchemeId = schemeId,
            SubSchemeId = subSchemeId2,
            SubSchemeShares = shares2
        });

        //remove sub scheme1
        {
            var removeSubSchemeResult = await creator.RemoveSubScheme.SendAsync(new RemoveSubSchemeInput
            {
                SchemeId = schemeId,
                SubSchemeId = subSchemeId1
            });
            removeSubSchemeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var scheme = await creator.GetScheme.CallAsync(schemeId);
            scheme.TotalShares.ShouldBe(shares2);
            scheme.SubSchemes.Count.ShouldBe(1);
            scheme.SubSchemes.First().Shares.ShouldBe(shares2);
        }
    }

    [Fact]
    public async Task ProfitContract_AddBeneficiary_Without_Authority_Test()
    {
        var creator = Creators[0];
        var receiver = Accounts[0].Address;
        var schemeId = await CreateSchemeAsync();
        //  without authorized
        {
            var senderWithoutAuthority = Creators[1];
            var ret = await senderWithoutAuthority.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = receiver,
                    Shares = 100
                },
                SchemeId = schemeId
            });
            ret.TransactionResult.Error.ShouldContain("Only manager");
        }
    }

    [Fact]
    public async Task ProfitContract_AddBeneficiary_With_Invalid_Input_Test()
    {
        var creator = Creators[0];
        var receiver = Accounts[0].Address;
        // input.SchemeId == null
        {
            var ret = await creator.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = receiver,
                    Shares = 1
                },
                SchemeId = null
            });
            ret.TransactionResult.Error.ShouldContain("Invalid scheme id");
        }
        var schemeId = await CreateSchemeAsync();

        // Invalid beneficiary address  
        {
            var ret = await creator.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = null,
                SchemeId = schemeId
            });
            ret.TransactionResult.Error.ShouldContain("Invalid beneficiary address");

            ret = await creator.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = null,
                    Shares = 1
                },
                SchemeId = schemeId
            });
            ret.TransactionResult.Error.ShouldContain("Invalid beneficiary address");
        }

        //Invalid share
        {
            var ret = await creator.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
            {
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = receiver,
                    Shares = -1
                },
                SchemeId = schemeId
            });
            ret.TransactionResult.Error.ShouldContain("Invalid share");
        }
    }

    [Fact]
    public async Task ProfitContract_AddWeight_Test()
    {
        var creator = Creators[0];

        var schemeId = await CreateSchemeAsync();

        const int shares1 = 100;
        const int shares2 = 200;
        var receiver1 = Accounts[0].Address;
        var receiver2 = Accounts[1].Address;

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = receiver1, Shares = shares1 },
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
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = receiver2, Shares = shares2 },
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
        var beneficiary = Accounts[0].Address;

        var schemeId = await CreateSchemeAsync();

        {
            var scheme = await creator.GetScheme.CallAsync(schemeId);
            scheme.CurrentPeriod.ShouldBe(1);
        }

        await ContributeProfits(schemeId);

        // Current period: 1
        {
            var executionResult = await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiary, Shares = shares },
                EndPeriod = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // We need to add Shares successfully for further testing.
        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            SchemeId = schemeId,
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiary, Shares = shares },
        });

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        {
            var scheme = await creator.GetScheme.CallAsync(schemeId);
            scheme.CurrentPeriod.ShouldBe(2);
        }

        // Current period: 2
        {
            var executionResult = await creator.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
            {
                SchemeId = schemeId,
                BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiary, Shares = shares },
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

        var executionResult = await creator.AddBeneficiary.SendWithExceptionAsync(new AddBeneficiaryInput
        {
            SchemeId = HashHelper.ComputeFrom("SchemeId"),
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = Accounts[0].Address, Shares = 100 },
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("not found.");
    }

    [Fact]
    public async Task ProfitContract_RemoveBeneficiary_Test()
    {
        const int shares = 100;
        const int amount = 100;

        var creator = Creators[0];
        var beneficiary = Normal[0];
        var receiverAddress = Address.FromPublicKey(NormalKeyPair[0].PublicKey);

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = receiverAddress, Shares = shares },
            SchemeId = schemeId,
            EndPeriod = 1
        });

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
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

        var executionResult = await creator.RemoveBeneficiary.SendWithExceptionAsync(new RemoveBeneficiaryInput
        {
            SchemeId = HashHelper.ComputeFrom("SchemeId"),
            Beneficiary = Accounts[0].Address
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("not found.");
    }

    [Fact]
    public async Task ProfitContract_RemoveBeneficiary_Without_Authority_Test()
    {
        var creator = Creators[0];
        var receiverAddress = Address.FromPublicKey(NormalKeyPair[0].PublicKey);
        var schemeId = await CreateSchemeAsync();

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare
            {
                Beneficiary = receiverAddress,
                Shares = 1
            },
            SchemeId = schemeId
        });
        var creatorWithoutAuthority = Creators[2];
        var ret = await creatorWithoutAuthority.RemoveBeneficiary.SendWithExceptionAsync(
            new RemoveBeneficiaryInput
            {
                SchemeId = schemeId,
                Beneficiary = receiverAddress
            });
        ret.TransactionResult.Error.ShouldContain("Only manager");
    }

    [Fact]
    public async Task ProfitContract_RemoveBeneficiary_With_Invalid_Input_Test()
    {
        var creator = Creators[0];
        var receiverAddress = Address.FromPublicKey(NormalKeyPair[0].PublicKey);
        var schemeId = await CreateSchemeAsync();

        //Invalid scheme id
        {
            var ret = await creator.RemoveBeneficiary.SendWithExceptionAsync(new RemoveBeneficiaryInput
            {
                SchemeId = null,
                Beneficiary = receiverAddress
            });
            ret.TransactionResult.Error.ShouldContain("Invalid scheme id");
        }

        //Invalid Beneficiary address.
        {
            var ret = await creator.RemoveBeneficiary.SendWithExceptionAsync(new RemoveBeneficiaryInput
            {
                SchemeId = schemeId,
                Beneficiary = null
            });
            ret.TransactionResult.Error.ShouldContain("Invalid Beneficiary address.");
        }
    }

    [Fact]
    public async Task ProfitContract_ReleaseProfits_WithoutEnoughBalance_Test()
    {
        const long amount = 100;

        var creator = Creators[0];

        var schemeId = await CreateSchemeAsync();

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = Accounts[0].Address, Shares = 100 },
            SchemeId = schemeId,
        });

        var executionResult = await creator.DistributeProfits.SendWithExceptionAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("Insufficient balance");
    }

    [Fact]
    public async Task ProfitContract_ReleaseProfits_InvalidTokenSymbol_Test()
    {
        const long amount = 100;
        var creator = Creators[0];
        var schemeId = await CreateSchemeAsync();
        var executionResult = await creator.DistributeProfits.SendWithExceptionAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { "", amount }
            },
            Period = 2
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("Invalid token symbol");
    }

    [Fact]
    public async Task ProfitContract_ReleaseProfits_InvalidPeriod_Test()
    {
        const long amount = 100;

        var creator = Creators[0];

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = Accounts[0].Address, Shares = 100 },
            SchemeId = schemeId,
        });

        var executionResult = await creator.DistributeProfits.SendWithExceptionAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 2
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("Invalid period.");
    }

    [Fact]
    public async Task ProfitContract_ReleaseProfits_NotCreator_Test()
    {
        const long amount = 100;

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId);

        // The actual creator is Creators[0]
        var anotherGuy = Creators[1];

        var executionResult = await anotherGuy.DistributeProfits.SendWithExceptionAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
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

        var executionResult = await user.DistributeProfits.SendWithExceptionAsync(new DistributeProfitsInput
        {
            SchemeId = HashHelper.ComputeFrom("SchemeId"),
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("not found.");
    }

    [Fact]
    public async Task ProfitContract_ReleaseProfits_Test()
    {
        const long amount = 100;

        var creator = Creators[0];

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId, amount * 2);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = Accounts[0].Address, Shares = 100 },
            SchemeId = schemeId,
        });

        // First time.
        {
            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { ProfitContractTestConstants.NativeTokenSymbol, amount }
                },
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Second time.
        {
            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { ProfitContractTestConstants.NativeTokenSymbol, amount }
                },
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

        var schemeId = await CreateSchemeAsync();
        var subSchemeId1 = await CreateSchemeAsync(1);
        var subSchemeId2 = await CreateSchemeAsync(2);

        await ContributeProfits(schemeId);

        // Check balance of main profit scheme.
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
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Check balance of first sub profit scheme.
        {
            var subProfitItem = await creator.GetScheme.CallAsync(subSchemeId1);
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = subProfitItem.VirtualAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;

            balance.ShouldBe(amount.Mul(weight1).Div(weight1 + weight2));
        }

        // Check balance of second sub profit scheme.
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

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress, Shares = shares },
            SchemeId = schemeId,
        });

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = beneficiaryAddress,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol
        })).Balance;
        balance.ShouldBe(amount + initialBalance);
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

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress1, Shares = weight1 },
            SchemeId = schemeId,
        });

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress2, Shares = weight2 },
            SchemeId = schemeId,
        });

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        await beneficiary1.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        // Check balance of Beneficiary 1.
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress1,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(weight1.Mul(amount).Div(weight1 + weight2).Add(initialBalance1));
        }

        await beneficiary2.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        // Check balance of Beneficiary 2.
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress2,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(weight2.Mul(amount).Div(weight1 + weight2).Add(initialBalance2));
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

        var schemeId = await CreateSchemeAsync();
        var subSchemeId = await CreateSchemeAsync(1);

        await ContributeProfits(schemeId);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress1, Shares = weight1 },
            SchemeId = schemeId,
        });

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress2, Shares = weight2 },
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
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        await beneficiary1.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        // Check balance of Beneficiary 1.
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress1,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(weight1.Mul(amount).Div(weight1.Add(weight2).Add(weight3)).Add(initialBalance1));
        }

        await beneficiary2.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        // Check balance of Beneficiary 2.
        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress2,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(weight2.Mul(amount).Div(weight1.Add(weight2).Add(weight3)).Add(initialBalance2));
        }
    }

    [Fact]
    public async Task ProfitContract_Profit_ProfitItemNotFound_Test()
    {
        var beneficiary = Normal[0];

        var executionResult = await beneficiary.ClaimProfits.SendWithExceptionAsync(new ClaimProfitsInput
        {
            SchemeId = HashHelper.ComputeFrom("SchemeId"),
        });

        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("not found.");
    }

    [Fact]
    public async Task ProfitContract_Profit_NotRegisteredBefore_Test()
    {
        const long amount = 100;

        var creator = Creators[0];
        var beneficiary = Normal[0];

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId);

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = schemeId,
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            Period = 1
        });

        var executionResult = await beneficiary.ClaimProfits.SendWithExceptionAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
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

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId, amount * periodCount + amount);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            SchemeId = schemeId,
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress, Shares = shares },
        });

        for (var i = 0; i < periodCount; i++)
        {
            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { ProfitContractTestConstants.NativeTokenSymbol, amount }
                },
                Period = i + 1
            });
        }

        await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount * periodCount + initialBalance);

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
            AmountsMap =
            {
                { ProfitContractTestConstants.NativeTokenSymbol, amount }
            },
            SchemeId = schemeId
        });

        await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount * periodCount + amount + initialBalance);

            var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = beneficiaryAddress
            });
            details.Details[0].LastProfitPeriod.ShouldBe(periodCount + 2);
        }
    }
        
    [Fact]
    public async Task ProfitContract_Profit_AfterMultiplePeriods_Test()
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

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId, amount * 2 * periodCount + amount);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            SchemeId = schemeId,
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = beneficiaryAddress, Shares = shares },
            EndPeriod = periodCount
        });

        for (var i = 0; i < periodCount * 2; i++)
        {
            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { ProfitContractTestConstants.NativeTokenSymbol, amount }
                },
                Period = i + 1
            });
        }

        await beneficiary.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = schemeId,
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = beneficiaryAddress,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Balance;
            balance.ShouldBe(amount * periodCount + initialBalance);

            var details = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = beneficiaryAddress
            });
            details.Details.Count.ShouldBe(0);
        }
    }

    private async Task<Hash> CreateSchemeAsync(int returnIndex = 0,
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

        var schemeId = await CreateSchemeAsync();

        await ContributeProfits(schemeId, amount * 2);

        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = Accounts[0].Address, Shares = 100 },
            SchemeId = schemeId,
        });

        // First time.
        {
            var executionResult = await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { ProfitContractTestConstants.NativeTokenSymbol, amount }
                },
                Period = 1
            });

            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var transactionResult = await ProfitContractStub.ContributeProfits.SendWithExceptionAsync(
                new ContributeProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Amount = 100,
                    Period = 1
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Invalid contributing period.");
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

    [Fact]
    public async Task ContributeProfits_With_Invalid_Input_Test()
    {
        var creator = Creators[0];
        var schemeId = await CreateSchemeAsync();

        // invalid token symbol
        {
            var transactionResult = await creator.ContributeProfits.SendWithExceptionAsync(
                new ContributeProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = ""
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("not exists");
        }

        // invalid token amount
        {
            var transactionResult = await creator.ContributeProfits.SendWithExceptionAsync(
                new ContributeProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Amount = 0
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Amount need to greater than 0.");
        }

        //invalid scheme id
        {
            var transactionResult = await creator.ContributeProfits.SendWithExceptionAsync(
                new ContributeProfitsInput
                {
                    SchemeId = new Hash(),
                    Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                    Amount = 10,
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("not found.");
        }
    }

    [Fact]
    public async Task ResetManager_Without_Authority_Test()
    {
        var schemeId = await CreateSchemeAsync();
        var sendWithoutAuthority = Creators[1];
        var resetRet = await sendWithoutAuthority.ResetManager.SendWithExceptionAsync(new ResetManagerInput
        {
            NewManager = new Address(),
            SchemeId = schemeId,
        });
        resetRet.TransactionResult.Error.ShouldContain("Only scheme manager can reset manager");
    }

    [Fact]
    public async Task ResetManager_With_Invalid_Input_Test()
    {
        var schemeId = await CreateSchemeAsync();
        var creator = Creators[0];
        var newManager = Address.FromPublicKey(CreatorKeyPair[1].PublicKey);
        var resetRet = await creator.ResetManager.SendWithExceptionAsync(new ResetManagerInput
        {
            NewManager = newManager,
            SchemeId = new Hash()
        });
        resetRet.TransactionResult.Error.ShouldContain("not found.");

        resetRet = await creator.ResetManager.SendWithExceptionAsync(new ResetManagerInput
        {
            NewManager = new Address(),
            SchemeId = schemeId
        });
        resetRet.TransactionResult.Error.ShouldContain("Invalid new sponsor.");
    }

    [Fact]
    public async Task ResetManager_Success_Test()
    {
        var schemeId = await CreateSchemeAsync();
        var creator = Creators[0];
        var newManager = Address.FromPublicKey(CreatorKeyPair[1].PublicKey);
        await creator.ResetManager.SendAsync(new ResetManagerInput
        {
            NewManager = newManager,
            SchemeId = schemeId
        });
        var schemeIdForInitialCreator = await ProfitContractStub.GetManagingSchemeIds.CallAsync(
            new GetManagingSchemeIdsInput
            {
                Manager = Address.FromPublicKey(CreatorKeyPair[0].PublicKey)
            });
        schemeIdForInitialCreator.SchemeIds.Count.ShouldBe(0);

        var schemeIdForNewManager = await ProfitContractStub.GetManagingSchemeIds.CallAsync(
            new GetManagingSchemeIdsInput
            {
                Manager = newManager
            });
        schemeIdForNewManager.SchemeIds.Count.ShouldBe(1);
        var schemeInfo = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        schemeInfo.Manager.Equals(newManager).ShouldBeTrue();
    }

    [Fact]
    public async Task GetManagingSchemeIds_Test()
    {
        var schemeId1 = await CreateSchemeAsync();
        var schemeId2 = await CreateSchemeAsync(1);
        var managerSchemes = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = Address.FromPublicKey(CreatorKeyPair[0].PublicKey)
        });
        managerSchemes.SchemeIds.Count.ShouldBe(2);
        managerSchemes.SchemeIds.Contains(schemeId1).ShouldBeTrue();
        managerSchemes.SchemeIds.Contains(schemeId2).ShouldBeTrue();
    }

    [Fact]
    public async Task GetProfitMapAndAmount_Test()
    {
        const long amount = 100;
        var creator = Creators[0];
        var schemeId = await CreateSchemeAsync();
        await ContributeProfits(schemeId, amount * 2);
        var receiver = Accounts[0].Address;
        var tokenSymbol = ProfitContractTestConstants.NativeTokenSymbol;
        await creator.AddBeneficiary.SendAsync(new AddBeneficiaryInput
        {
            BeneficiaryShare = new BeneficiaryShare { Beneficiary = receiver, Shares = 100 },
            SchemeId = schemeId,
        });

        //first time
        {
            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { tokenSymbol, amount }
                },
                Period = 1
            });
            var profitAmount = await ProfitContractStub.GetProfitAmount.CallAsync(new GetProfitAmountInput
            {
                Beneficiary = receiver,
                Symbol = tokenSymbol,
                SchemeId = schemeId
            });
            profitAmount.Value.ShouldBe(amount);
            var profitMap = await ProfitContractStub.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = receiver
            });
            profitMap.Value[tokenSymbol].ShouldBe(amount);
        }

        // after claim
        {
            await ProfitContractStub.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId
            });
            var details = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                Beneficiary = receiver,
                SchemeId = schemeId
            });
            var scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
            details.Details.First().LastProfitPeriod.ShouldBe(scheme.CurrentPeriod);
            var profitAmount = await ProfitContractStub.GetProfitAmount.CallAsync(new GetProfitAmountInput
            {
                Beneficiary = receiver,
                Symbol = tokenSymbol,
                SchemeId = schemeId
            });
            profitAmount.Value.ShouldBe(0);
            var profitMap = await ProfitContractStub.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = receiver
            });
            profitMap.Value.ShouldNotContainKey(tokenSymbol);
        }

        //second time
        {
            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                AmountsMap =
                {
                    { tokenSymbol, amount }
                },
                Period = 2
            });
            var profitAmount = await ProfitContractStub.GetProfitAmount.CallAsync(new GetProfitAmountInput
            {
                Beneficiary = receiver,
                Symbol = tokenSymbol,
                SchemeId = schemeId
            });
            profitAmount.Value.ShouldBe(amount);
            var profitMap = await ProfitContractStub.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = receiver
            });
            profitMap.Value[tokenSymbol].ShouldBe(amount);

            await ProfitContractStub.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = schemeId
            });
            var details = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                Beneficiary = receiver,
                SchemeId = schemeId
            });
            var scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
            details.Details.First().LastProfitPeriod.ShouldBe(scheme.CurrentPeriod);
        }
    }
    
    [Fact]
    public async Task IncreaseBackupSubsidyTotalShare_Test()
    {
        var schemeId = await CreateSchemeAsync();
        var scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        scheme.TotalShares.ShouldBe(0);
        await ProfitContractStub.IncreaseBackupSubsidyTotalShare.SendAsync(schemeId);
        scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        scheme.TotalShares.ShouldBe(1);
        var result = await ProfitContractStub.IncreaseBackupSubsidyTotalShare.SendWithExceptionAsync(schemeId);
        result.TransactionResult.Error.ShouldContain("Already increased");
    }

    private async Task ContributeProfits(Hash schemeId, long amount = 100)
    {
        await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeId = schemeId,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol,
            Amount = amount
        });
    }
}