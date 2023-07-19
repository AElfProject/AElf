using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.CSharp.Core;
using AElf.Standards.ACS1;
using AElf.Standards.ACS10;
using AElf.Standards.ACS3;
using AElf.Standards.ACS5;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT;

public partial class EconomicSystemTest : EconomicSystemTestBase
{
    public EconomicSystemTest()
    {
        InitializeContracts();
    }

    [Fact]
    public async Task EconomistSystem_CheckBasicInformation_Test()
    {
        var symbolList = await TreasuryContractStub.GetSymbolList.CallAsync(new Empty());
        symbolList.Value.Count.ShouldBe(1);
        symbolList.Value[0].ShouldBe(EconomicContractsTestConstants.NativeTokenSymbol);

        // Treasury contract created Treasury profit scheme and set Profit Id to Profit Contract.
        var treasuryProfit = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
        treasuryProfit.Manager.ShouldBe(TreasuryContractAddress);
        treasuryProfit.SubSchemes.Count.ShouldBe(3);
        treasuryProfit.IsReleaseAllBalanceEveryTimeByDefault.ShouldBe(true);
    }

    [Fact]
    public async Task EconomistSystem_SetMethodCallThreshold_Test()
    {
        const long feeAmount = 100L;
        var setMethodResult = await MethodCallThresholdContractStub.SetMethodCallingThreshold.SendAsync(
            new SetMethodCallingThresholdInput
            {
                Method = nameof(MethodCallThresholdContractStub.SendForFun),
                SymbolToAmount = { { EconomicSystemTestConstants.NativeTokenSymbol, feeAmount } }
            });
        setMethodResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var tokenAmount = await MethodCallThresholdContractStub.GetMethodCallingThreshold.CallAsync(new StringValue
        {
            Value = nameof(MethodCallThresholdContractStub.SendForFun)
        });
        tokenAmount.SymbolToAmount[EconomicSystemTestConstants.NativeTokenSymbol].ShouldBe(feeAmount);
    }

    [Fact]
    public async Task Treasury_Dividend_Pool_Weight_Update_To_Miner_Reward_Weight_Test()
    {
        var newWeightSetting = new DividendPoolWeightSetting
        {
            BackupSubsidyWeight = 1,
            CitizenWelfareWeight = 1,
            MinerRewardWeight = 8
        };
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress,
            nameof(TreasuryContractStub.SetDividendPoolWeightSetting), newWeightSetting);
        var minerRewardProfit =
            await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
        var subSchemes = minerRewardProfit.SubSchemes;
        subSchemes.Count.ShouldBe(3);
        var minerRewardWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
        var basicMinerRewardScheme = subSchemes.Single(x =>
            x.SchemeId == minerRewardWeightSetting.BasicMinerRewardProportionInfo.SchemeId);
        basicMinerRewardScheme.Shares.ShouldBe(2);
        var reElectionRewardScheme = subSchemes.Single(x =>
            x.SchemeId == minerRewardWeightSetting.WelcomeRewardProportionInfo.SchemeId);
        reElectionRewardScheme.Shares.ShouldBe(1);
        var votesWeightRewardScheme = subSchemes.Single(x =>
            x.SchemeId == minerRewardWeightSetting.FlexibleRewardProportionInfo.SchemeId);
        votesWeightRewardScheme.Shares.ShouldBe(1);
    }

    [Fact]
    public async Task TreasuryContract_InitialTreasuryContract_Repeat_Test()
    {
        var repeatInitializeRet = await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
        repeatInitializeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        repeatInitializeRet.TransactionResult.Error.ShouldContain("Already initialized");
    }

    [Fact]
    public async Task TreasuryContract_InitialMiningRewardProfitItem_Repeat_Test()
    {
        var repeatInitializeRet = await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());
        repeatInitializeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        repeatInitializeRet.TransactionResult.Error.ShouldContain("Already initialized");
    }

    [Fact]
    public async Task TreasuryContract_Release_Without_Authority_Test()
    {
        var releaseRet = await TreasuryContractStub.Release.SendAsync(new ReleaseInput());
        releaseRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        releaseRet.TransactionResult.Error.ShouldContain(
            "nly AElf Consensus Contract can release profits from Treasury");
    }

    [Fact]
    public async Task TreasuryContract_InitialMiningRewardProfitItem_Success_Test()
    {
        var treasurySchemeId = await TreasuryContractStub.GetTreasurySchemeId.CallAsync(new Empty());
        treasurySchemeId.ShouldBe(ProfitItemsIds[ProfitType.Treasury]);
        var defaultDividendPoolWeightSetting =
            await TreasuryContractStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
        defaultDividendPoolWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(5);
        defaultDividendPoolWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(75);
        defaultDividendPoolWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(20);

        var defaultMinerRewardWeightSetting =
            await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
        defaultMinerRewardWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(50);
        defaultMinerRewardWeightSetting.WelcomeRewardProportionInfo.Proportion.ShouldBe(25);
        defaultMinerRewardWeightSetting.FlexibleRewardProportionInfo.Proportion.ShouldBe(25);

        var treasuryScheme = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = TreasuryContractAddress
        });
        var electionScheme = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = ElectionContractAddress
        });
        treasuryScheme.SchemeIds.Count.ShouldBe(5);
        electionScheme.SchemeIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task TreasuryContract_Donate_With_Invalid_Input_Test()
    {
        var donateRet = await TreasuryContractStub.Donate.SendAsync(new DonateInput
        {
            Amount = 0
        });
        donateRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        donateRet.TransactionResult.Error.ShouldContain("Invalid amount of donating");
    }

    [Fact]
    public async Task TreasuryContract_Donate_Success_Test()
    {
        // donate resource token
        var resourceTokenSymbol = ResourceTokenSymbol;
        var nativeTokenSymbol = EconomicContractsTestConstants.NativeTokenSymbol;
        var startResourceTokenDeposit = await TokenConverterContractStub.GetDepositConnectorBalance.CallAsync(
            new StringValue
            {
                Value = resourceTokenSymbol
            });
        var balanceOfResourceTokenSymbol = await GetBalanceAsync(resourceTokenSymbol, BootMinerAddress);
        balanceOfResourceTokenSymbol.ShouldBe(0);
        var buyAmount = 1_00000000;
        var approveAmount = 1_00000000;
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Symbol = nativeTokenSymbol,
            Amount = approveAmount,
            Spender = TokenConverterContractAddress
        });
        var buyRet = await TokenConverterContractStub.Buy.SendAsync(new BuyInput
        {
            Symbol = resourceTokenSymbol,
            Amount = buyAmount
        });
        buyRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var depositBeforeDonate = await TokenConverterContractStub.GetDepositConnectorBalance.CallAsync(
            new StringValue
            {
                Value = ResourceTokenSymbol
            });
        var nativeTokenCost = depositBeforeDonate.Value.Sub(startResourceTokenDeposit.Value);
        var treasuryVirtualAddress = await GetTreasurySchemeVirtualAddressAsync();
        var balanceOfTreasuryBeforeDonate = await GetBalanceAsync(nativeTokenSymbol, treasuryVirtualAddress);
        //donate
        var donateRet = await TreasuryContractStub.Donate.SendAsync(new DonateInput
        {
            Symbol = resourceTokenSymbol,
            Amount = buyAmount
        });
        donateRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var depositAfterDonate = await TokenConverterContractStub.GetDepositConnectorBalance.CallAsync(
            new StringValue
            {
                Value = ResourceTokenSymbol
            });
        depositBeforeDonate.Value.Sub(depositAfterDonate.Value).ShouldBe(nativeTokenCost);
        var feeRateString = EconomicContractsTestConstants.TokenConverterFeeRate;
        var feeRate = decimal.Parse(feeRateString);
        var balanceOfTreasuryAfterDonate = await GetBalanceAsync(nativeTokenSymbol, treasuryVirtualAddress);
        balanceOfTreasuryAfterDonate.ShouldBe(nativeTokenCost.Add(balanceOfTreasuryBeforeDonate));
    }

    [Fact]
    public async Task Treasury_ChangeTreasuryController_Fail_Test()
    {
        // without authority
        {
            var changeRet = await TreasuryContractStub.ChangeTreasuryController.SendAsync(new AuthorityInfo());
            changeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeRet.TransactionResult.Error.ShouldContain("no permission");
        }

        // organization does not exist
        {
            var newController = new AuthorityInfo
            {
                OwnerAddress = ParliamentContractAddress,
                ContractAddress = ParliamentContractAddress
            };
            var changeRet = await ExecuteProposalForParliamentTransactionWithException(Tester, TreasuryContractAddress,
                nameof(TreasuryContractStub.ChangeTreasuryController), newController);
            changeRet.Status.ShouldBe(TransactionResultStatus.Failed);
            changeRet.Error.ShouldContain("Invalid authority input");
        }
    }

    [Fact]
    public async Task Treasury_ChangeTreasuryController_Success_Test()
    {
        var newParliament = new CreateOrganizationInput
        {
            ProposerAuthorityRequired = false,
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MaximalAbstentionThreshold = 1,
                MaximalRejectionThreshold = 1,
                MinimalApprovalThreshold = 1,
                MinimalVoteThreshold = 1
            },
            ParliamentMemberProposingAllowed = false
        };
        var createNewParliament =
            (await ParliamentContractStub.CreateOrganization.SendAsync(newParliament)).TransactionResult;
        createNewParliament.Status.ShouldBe(TransactionResultStatus.Mined);
        var calculatedNewParliamentAddress =
            await ParliamentContractStub.CalculateOrganizationAddress.CallAsync(newParliament);
        var newController = new AuthorityInfo
        {
            ContractAddress = ParliamentContractAddress,
            OwnerAddress = calculatedNewParliamentAddress
        };
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress,
            nameof(TreasuryContractStub.ChangeTreasuryController), newController);
        var getController = await TreasuryContractStub.GetTreasuryController.CallAsync(new Empty());
        getController.ContractAddress.ShouldBe(newController.ContractAddress);
        getController.OwnerAddress.ShouldBe(newController.OwnerAddress);
    }

    [Fact]
    public async Task Treasury_SetSymbolList_Without_Authority()
    {
        var setRet = await TreasuryContractStub.SetSymbolList.SendAsync(new SymbolList());
        setRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        setRet.TransactionResult.Error.ShouldContain("no permission");
    }

    [Fact]
    public async Task Treasury_SetSymbolList_With_Invalid_Token_Test()
    {
        var tokenSymbol = "TEST";
        var nativeTokenSymbol = EconomicContractsTestConstants.NativeTokenSymbol;
        var resourceTokenSymbol = ResourceTokenSymbol;
        var methodName = nameof(TreasuryContractStub.SetSymbolList);
        var tokenCreateInput = new CreateInput
        {
            Symbol = tokenSymbol,
            TokenName = "TEST name",
            TotalSupply = 1_0000_0000,
            Issuer = BootMinerAddress,
            IsBurnable = true,
            Owner = BootMinerAddress
        };
        await ExecuteProposalForParliamentTransactionWithException(Tester, TokenContractAddress, nameof(TokenContractStub.Create),
            tokenCreateInput);
        // without native token
        {
            var newSymbolList = new SymbolList
            {
                Value =
                {
                    tokenSymbol
                }
            };
            var setSymbolRet =
                await ExecuteProposalForParliamentTransactionWithException(Tester, TreasuryContractAddress, methodName,
                    newSymbolList);
            setSymbolRet.Error.ShouldContain("Need to contain native symbol");
        }

        //not valid connector
        {
            var newSymbolList = new SymbolList
            {
                Value =
                {
                    nativeTokenSymbol, resourceTokenSymbol
                }
            };
            var setSymbolRet =
                await ExecuteProposalForParliamentTransactionWithException(Tester, TreasuryContractAddress, methodName,
                    newSymbolList);
            setSymbolRet.Error.ShouldContain($"Token {resourceTokenSymbol} doesn't need to set to symbol list");
        }
    }

    [Fact]
    public async Task Treasury_SetSymbolList_Success_Test()
    {
        var tokenSymbol = "CWJ";
        var nativeTokenSymbol = EconomicContractsTestConstants.NativeTokenSymbol;
        var methodName = nameof(TreasuryContractStub.SetSymbolList);
        var tokenCreateInput = new CreateInput
        {
            Symbol = tokenSymbol,
            TokenName = "CWJ name",
            TotalSupply = 1_0000_0000,
            Issuer = BootMinerAddress,
            IsBurnable = true,
            Owner = BootMinerAddress
        };
        await ExecuteProposalForParliamentTransaction(TokenContractAddress, nameof(TokenContractStub.Create),
            tokenCreateInput);
        var newSymbolList = new SymbolList
        {
            Value =
            {
                nativeTokenSymbol, tokenSymbol
            }
        };
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress, methodName,
            newSymbolList);
        var getSymbolList = await TreasuryContractStub.GetSymbolList.CallAsync(new Empty());
        getSymbolList.Value.Count.ShouldBe(2);
        getSymbolList.Value.Contains(tokenSymbol).ShouldBeTrue();
    }

    [Fact]
    public async Task Treasury_SetDividendPoolWeightSetting_Fail_Test()
    {
        // No permission
        {
            var setRet =
                await TreasuryContractStub.SetDividendPoolWeightSetting.SendAsync(new DividendPoolWeightSetting());
            setRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            setRet.TransactionResult.Error.ShouldContain("no permission");
        }

        // Invalid input
        {
            var methodName = nameof(TreasuryContractStub.SetDividendPoolWeightSetting);
            var newDividendSetting = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 0
            };
            var setRet = await ExecuteProposalForParliamentTransactionWithException(Tester, TreasuryContractAddress,
                methodName,
                newDividendSetting);
            setRet.Status.ShouldBe(TransactionResultStatus.Failed);
            setRet.Error.ShouldContain("invalid input");
        }
    }

    [Fact]
    public async Task Treasury_SetDividendPoolWeightSetting_Success_Test()
    {
        var defaultWeightSetting = await TreasuryContractStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
        defaultWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(5);
        defaultWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(75);
        defaultWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(20);
        var newWeightSetting = new DividendPoolWeightSetting
        {
            BackupSubsidyWeight = 1,
            CitizenWelfareWeight = 1,
            MinerRewardWeight = 8
        };
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress,
            nameof(TreasuryContractStub.SetDividendPoolWeightSetting), newWeightSetting);
        var updatedWeightSetting = await TreasuryContractStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
        updatedWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(10);
        updatedWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(10);
        updatedWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(80);
        var treasuryProfit = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
        var subSchemes = treasuryProfit.SubSchemes;
        subSchemes.Count.ShouldBe(3);
        var backSubsidyScheme =
            subSchemes.Single(x => x.SchemeId == updatedWeightSetting.BackupSubsidyProportionInfo.SchemeId);
        backSubsidyScheme.Shares.ShouldBe(1);
        var citizenWelfareScheme =
            subSchemes.Single(x => x.SchemeId == updatedWeightSetting.CitizenWelfareProportionInfo.SchemeId);
        citizenWelfareScheme.Shares.ShouldBe(1);
        var minerRewardScheme =
            subSchemes.Single(x => x.SchemeId == updatedWeightSetting.MinerRewardProportionInfo.SchemeId);
        minerRewardScheme.Shares.ShouldBe(8);
    }

    [Fact]
    public async Task Treasury_SetMinerRewardWeightSetting_Fail_Test()
    {
        //without authority
        {
            var setRet =
                await TreasuryContractStub.SetMinerRewardWeightSetting.SendAsync(new MinerRewardWeightSetting());
            setRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            setRet.TransactionResult.Error.ShouldContain("no permission");
        }

        //invalid Inpout
        {
            var methodName = nameof(TreasuryContractStub.SetMinerRewardWeightSetting);
            var newRewardWeightSetting = new MinerRewardWeightSetting
            {
                BasicMinerRewardWeight = 0
            };
            var setRet = await ExecuteProposalForParliamentTransactionWithException(Tester, TreasuryContractAddress,
                methodName,
                newRewardWeightSetting);
            setRet.Status.ShouldBe(TransactionResultStatus.Failed);
            setRet.Error.ShouldContain("invalid input");
        }
    }

    [Fact]
    public async Task Treasury_SetMinerRewardWeightSetting_Success_Test()
    {
        var defaultWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
        defaultWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(50);
        defaultWeightSetting.WelcomeRewardProportionInfo.Proportion.ShouldBe(25);
        defaultWeightSetting.FlexibleRewardProportionInfo.Proportion.ShouldBe(25);
        var newWeightSetting = new MinerRewardWeightSetting
        {
            BasicMinerRewardWeight = 1,
            WelcomeRewardWeight = 1,
            FlexibleRewardWeight = 8
        };
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress,
            nameof(TreasuryContractStub.SetMinerRewardWeightSetting), newWeightSetting);

        var updatedWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
        updatedWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(10);
        updatedWeightSetting.WelcomeRewardProportionInfo.Proportion.ShouldBe(10);
        updatedWeightSetting.FlexibleRewardProportionInfo.Proportion.ShouldBe(80);

        var minerRewardProfit =
            await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
        var subSchemes = minerRewardProfit.SubSchemes;
        subSchemes.Count.ShouldBe(3);
        var basicMinerRewardScheme = subSchemes.Single(x =>
            x.SchemeId == updatedWeightSetting.BasicMinerRewardProportionInfo.SchemeId);
        basicMinerRewardScheme.Shares.ShouldBe(1);
        var welcomeRewardScheme = subSchemes.Single(x =>
            x.SchemeId == updatedWeightSetting.WelcomeRewardProportionInfo.SchemeId);
        welcomeRewardScheme.Shares.ShouldBe(1);
        var flexibleRewardScheme = subSchemes.Single(x =>
            x.SchemeId == updatedWeightSetting.FlexibleRewardProportionInfo.SchemeId);
        flexibleRewardScheme.Shares.ShouldBe(8);
    }

    [Fact]
    public async Task Treasury_ChangeMethodFeeController_Test()
    {
        var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
            new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1000,
                    MinimalVoteThreshold = 1000
                }
            });

        var organizationAddress = createOrganizationResult.Output;

        var methodFeeController = await TreasuryContractStub.GetMethodFeeController.CallAsync(new Empty());
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress,
            nameof(TreasuryContractStub.ChangeMethodFeeController),
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = methodFeeController.ContractAddress
            });

        var newMethodFeeController = await TreasuryContractStub.GetMethodFeeController.CallAsync(new Empty());
        newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
    }


    [Fact]
    public async Task Treasury_ChangeMethodFeeController_With_Invalid_Authority_Test()
    {
        var newController = new AuthorityInfo
        {
            OwnerAddress = ElectionContractAddress,
            ContractAddress = ParliamentContractAddress
        };
        var proposalCreationMethodName = nameof(ElectionContractStub.ChangeMethodFeeController);
        var changeRet = await ExecuteProposalForParliamentTransactionWithException(Tester, TreasuryContractAddress,
            nameof(TreasuryContractStub.ChangeMethodFeeController), newController);
        changeRet.Status.ShouldBe(TransactionResultStatus.Failed);
        changeRet.Error.ShouldContain("Invalid authority input");
    }

    [Fact]
    public async Task Treasury_ChangeMethodFeeController_WithoutAuth_Test()
    {
        var newController = new AuthorityInfo
        {
            OwnerAddress = ElectionContractAddress,
            ContractAddress = ParliamentContractAddress
        };
        var result = await TreasuryContractStub.ChangeMethodFeeController.SendAsync(newController);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("Unauthorized behavior");
    }

    [Fact]
    public async Task SetMethodFee_With_Invalid_Input_Test()
    {
        // Invalid amount
        {
            var setMethodFeeRet = await TreasuryContractStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = "Test",
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = "NOTEXIST",
                        BasicFee = -111
                    }
                }
            });
            setMethodFeeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Invalid amount.");
        }

        // token does not exist
        {
            var setMethodFeeRet = await TreasuryContractStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = "Test",
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = "NOTEXIST",
                        BasicFee = 111
                    }
                }
            });
            setMethodFeeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Token is not found.");
        }
    }

    [Fact]
    public async Task SetMethodFee_Without_Authority_Test()
    {
        var tokenSymbol = "ELF";
        var methodName = "Test";
        var basicFee = 111;
        var setMethodFeeRet = await TreasuryContractStub.SetMethodFee.SendAsync(new MethodFees
        {
            MethodName = methodName,
            Fees =
            {
                new MethodFee
                {
                    Symbol = tokenSymbol,
                    BasicFee = basicFee
                }
            }
        });
        setMethodFeeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        setMethodFeeRet.TransactionResult.Error.ShouldContain("Unauthorized to set method fee.");
    }

    [Fact]
    public async Task SetMethodFee_Success_Test()
    {
        var tokenSymbol = "ELF";
        var methodName = "Test";
        var basicFee = 111;
        var newMethodFees = new MethodFees
        {
            MethodName = methodName,
            Fees =
            {
                new MethodFee
                {
                    Symbol = tokenSymbol,
                    BasicFee = basicFee
                }
            }
        };
        const string proposalCreationMethodName = nameof(TreasuryContractStub.SetMethodFee);
        await ExecuteProposalForParliamentTransaction(TreasuryContractAddress, proposalCreationMethodName,
            newMethodFees);
        var getMethodFee = await TreasuryContractStub.GetMethodFee.CallAsync(new StringValue
        {
            Value = methodName
        });
        getMethodFee.Fees.Count.ShouldBe(1);
        getMethodFee.Fees[0].Symbol.ShouldBe(tokenSymbol);
        getMethodFee.Fees[0].BasicFee.ShouldBe(basicFee);
    }

    private async Task<Address> GetTreasurySchemeVirtualAddressAsync()
    {
        var treasurySchemeId = await TreasuryContractStub.GetTreasurySchemeId.CallAsync(new Empty());
        var treasuryScheme = await ProfitContractStub.GetScheme.CallAsync(treasurySchemeId);
        return treasuryScheme.VirtualAddress;
    }

    private async Task<long> GetBalanceAsync(string symbol, Address owner)
    {
        var balanceResult = await TokenContractStub.GetBalance.CallAsync(
            new GetBalanceInput
            {
                Owner = owner,
                Symbol = symbol
            });
        return balanceResult.Balance;
    }
}