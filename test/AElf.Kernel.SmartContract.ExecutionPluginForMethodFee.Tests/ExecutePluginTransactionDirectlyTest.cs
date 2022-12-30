using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

public class ExecutePluginTransactionDirectlyTest : ExecutePluginTransactionDirectlyForMethodFeeTestBase
{
    [Fact]
    public async Task ChargeTransactionFees_Invalid_Input_Test()
    {
        // contract address should not be null
        {
            var ret =
                await TokenContractStub.ChargeTransactionFees.SendWithExceptionAsync(new ChargeTransactionFeesInput
                {
                    MethodName = "asd"
                });
            ret.TransactionResult.Error.ShouldContain("Invalid charge transaction fees input");
        }
    }

    [Fact]
    public async Task ChargeTransactionFees_Without_Primary_Token_Test()
    {
        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, 100000_00000000);
        var address = DefaultSender;
        var nativeTokenSymbol = NativeTokenSymbol;
        var methodName = nameof(TokenContractContainer.TokenContractStub.Create);

        // input With Primary Token
        await SetPrimaryTokenSymbolAsync();
        var beforeChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
        {
            ContractAddress = TokenContractAddress,
            MethodName = methodName,
        });
        chargeFeeRet.Output.Success.ShouldBe(true);
        var afterChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
        afterChargeBalance.ShouldBeLessThan(beforeChargeBalance);
    }

    [Fact]
    public async Task Set_Repeat_Token_Test()
    {
        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, 100000_00000000);
        await SetPrimaryTokenSymbolAsync();
        var address = DefaultSender;
        var methodName = nameof(TokenContractContainer.TokenContractStub.Transfer);
        var basicMethodFee = 1000;
        var methodFee = new MethodFees
        {
            MethodName = methodName,
            Fees =
            {
                new MethodFee
                {
                    Symbol = NativeTokenSymbol,
                    BasicFee = basicMethodFee
                },
                new MethodFee
                {
                    Symbol = NativeTokenSymbol,
                    BasicFee = basicMethodFee
                }
            }
        };
        var sizeFee = 0;
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);
        var beforeChargeBalance = await GetBalanceAsync(address, NativeTokenSymbol);
        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = methodName,
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBeTrue();
        var afterChargeBalance = await GetBalanceAsync(address, NativeTokenSymbol);
        beforeChargeBalance.Sub(afterChargeBalance).ShouldBe(basicMethodFee.Add(basicMethodFee));
    }


    // 1 => ELF  2 => CWJ  3 => YPA   method fee : native token: 1000
    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 10000L, 0, 0 }, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, 1000, "ELF", 2000,
        true)]
    [InlineData(new[] { 2, 1, 3 }, new[] { 10000L, 10000L, 0 }, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, 1000, "CWJ", 1000,
        true)]
    [InlineData(new[] { 2, 1, 3 }, new[] { 10000L, 10000L, 0 }, new[] { 1, 1, 1 }, new[] { 2, 1, 1 }, 1000, "CWJ", 2000,
        true)]
    [InlineData(new[] { 2, 1, 3 }, new[] { 10000L, 10000L, 0 }, new[] { 4, 1, 1 }, new[] { 2, 1, 1 }, 1000, "CWJ", 500,
        true)]
    [InlineData(new[] { 2, 1, 3 }, new[] { 100L, 1000L, 0 }, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, 1000, "CWJ", 100,
        false)]
    [InlineData(new[] { 3, 1, 2 }, new[] { 10L, 1000L, 100 }, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, 1000, "YPA", 10,
        false)]
    public async Task ChargeTransactionFees_With_Different_Transaction_Size_Fee_Token(int[] order, long[] balance,
        int[] baseWeight, int[] tokenWeight, long sizeFee, string chargeSymbol, long chargeAmount, bool isSuccess)
    {
        await SetPrimaryTokenSymbolAsync();

        var methodName = nameof(TokenContractContainer.TokenContractStub.Transfer);
        var basicMethodFee = 1000;
        var methodFee = new MethodFees
        {
            MethodName = methodName,
            Fees =
            {
                new MethodFee
                {
                    Symbol = NativeTokenSymbol,
                    BasicFee = basicMethodFee
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);
        var tokenSymbolList = new[] { NativeTokenSymbol, "CWJ", "YPA" };
        var tokenCount = 3;
        var orderedSymbolList = new string[tokenCount];
        var index = 0;
        foreach (var o in order)
        {
            orderedSymbolList[index++] = tokenSymbolList[o - 1];
        }

        var sizeFeeSymbolList = new SymbolListToPayTxSizeFee();
        for (var i = 0; i < tokenCount; i++)
        {
            var tokenSymbol = orderedSymbolList[i];
            if (tokenSymbol != NativeTokenSymbol)
                await CreateTokenAsync(DefaultSender, tokenSymbol);
            if (balance[i] > 0)
                await IssueTokenToDefaultSenderAsync(tokenSymbol, balance[i]);
            sizeFeeSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
            {
                TokenSymbol = tokenSymbol,
                AddedTokenWeight = tokenWeight[i],
                BaseTokenWeight = baseWeight[i]
            });
        }

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

        var beforeBalanceList = await GetDefaultBalancesAsync(orderedSymbolList);
        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = methodName,
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(isSuccess);
        var afterBalanceList = await GetDefaultBalancesAsync(orderedSymbolList);
        for (var i = 0; i < tokenCount; i++)
        {
            var balanceDiff = beforeBalanceList[i] - afterBalanceList[i];
            if (orderedSymbolList[i] == chargeSymbol)
                balanceDiff.ShouldBe(chargeAmount);
            else
            {
                if (orderedSymbolList[i] == NativeTokenSymbol)
                    balanceDiff -= basicMethodFee;
                balanceDiff.ShouldBe(0);
            }
        }
    }

    [Theory]
    [InlineData(new[] { 100L, 100, 100 }, new[] { 100L, 100, 100 }, new[] { 0L, 0, 0 }, true)]
    [InlineData(new[] { 100L, 100, 100 }, new[] { 20L, 30, 40 }, new[] { 80L, 70, 60 }, true)]
    [InlineData(new[] { 100L, 100, 100 }, new[] { 120L, 130, 140 }, new[] { 0L, 0, 0 }, true)]
    [InlineData(new[] { 100L, 100, 100 }, new[] { 100L, 100, 100 }, new[] { 0L, 0, 0 }, false)]
    [InlineData(new[] { 100L, 100, 100 }, new[] { 20L, 30, 40 }, new[] { 80L, 70, 60 }, false)]
    [InlineData(new[] { 100L, 100, 100 }, new[] { 120L, 130, 140 }, new[] { 0L, 0, 0 }, false)]
    public async Task DonateResourceToken_Test(long[] initialBalances, long[] tokenFee, long[] lastBalances,
        bool isMainChain)
    {
        var symbolList = new[] { "WEO", "CWJ", "YPA" };
        var feeMap = new TotalResourceTokensMaps();
        for (var i = 0; i < symbolList.Length; i++)
        {
            await CreateTokenAsync(DefaultSender, symbolList[i]);
            await IssueTokenToDefaultSenderAsync(symbolList[i], initialBalances[i]);
            feeMap.Value.Add(new ContractTotalResourceTokens
            {
                ContractAddress = DefaultSender,
                TokensMap = new TotalResourceTokensMap
                {
                    Value = { { symbolList[i], tokenFee[i] } }
                }
            });
        }

        if (!isMainChain)
        {
            var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain),
                new InitializeFromParentChainInput
                {
                    Creator = defaultParliament
                });
        }

        await TokenContractStub.DonateResourceToken.SendAsync(feeMap);

        for (var i = 0; i < symbolList.Length; i++)
        {
            var balance = await GetBalanceAsync(DefaultSender, symbolList[i]);
            balance.ShouldBe(lastBalances[i]);
            if (!isMainChain)
            {
                var consensusBalance = await GetBalanceAsync(ConsensusContractAddress, symbolList[i]);
                consensusBalance.ShouldBe(initialBalances[i] - lastBalances[i]);
            }
        }
    }

    [Fact]
    public async Task ClaimTransactionFee_Balance_WithOut_Receiver_Test()
    {
        var tokenSymbol = "JAN";
        var feeAmount = 10000;
        await CreateTokenAsync(DefaultSender, tokenSymbol);
        var beforeBurned = await GetTokenSupplyAmount(tokenSymbol);
        var claimFeeInput = new TotalTransactionFeesMap
        {
            Value =
            {
                { tokenSymbol, feeAmount }
            }
        };
        await TokenContractStub.ClaimTransactionFees.SendAsync(claimFeeInput);
        var afterBurned = await GetTokenSupplyAmount(tokenSymbol);
        (beforeBurned - afterBurned).ShouldBe(feeAmount);
    }

    [Fact]
    public async Task ClaimTransactionFee_Balance_With_Receiver_Test()
    {
        var tokenSymbol = "JAN";
        var feeAmount = 10000;
        await CreateTokenAsync(DefaultSender, tokenSymbol);
        var receiver = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var input = new InitializeFromParentChainInput
        {
            Creator = receiver
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.InitializeFromParentChain), input);
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetFeeReceiver), receiver);
        var beforeBurned = await GetTokenSupplyAmount(tokenSymbol);
        var beforeBalance = await GetBalanceAsync(receiver, tokenSymbol);
        var claimFeeInput = new TotalTransactionFeesMap
        {
            Value =
            {
                { tokenSymbol, feeAmount }
            }
        };
        await TokenContractStub.ClaimTransactionFees.SendAsync(claimFeeInput);
        var afterBurned = await GetTokenSupplyAmount(tokenSymbol);
        var afterBalance = await GetBalanceAsync(receiver, tokenSymbol);
        var shouldBurned = feeAmount.Div(10);
        (beforeBurned - afterBurned).ShouldBe(shouldBurned);
        (afterBalance - beforeBalance).ShouldBe(feeAmount - shouldBurned);
    }

    [Theory]
    [InlineData(10000, 10000, 200, 50, 50, 100, 0, 10000)]
    [InlineData(10000, 10000, 1000, 10, 10, 100, 960, 10000)]
    [InlineData(10000, 20000, 100, 100, 100, 100, 0, 19700)]
    [InlineData(100000, 10000, 1000, 100, 100, 100, 0, 9600)]
    [InlineData(0, 200, 0, 50, 50, 100, 0, 0)]
    [InlineData(10000, 10000, 200, 100, 100, 0, 0, 10000)]
    public async Task FreeAllowancesTest(long threshold, long initialBalance, long freeAmount, long basicFee,
        long sizeFee, long refreshSeconds,
        long newFreeAllowance, long afterBalance)
    {
        await SetPrimaryTokenSymbolAsync();
        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractStub.ConfigMethodFeeFreeAllowances), new MethodFeeFreeAllowancesConfig
            {
                FreeAllowances = new MethodFeeFreeAllowances
                {
                    Value =
                    {
                        new MethodFeeFreeAllowance
                        {
                            Symbol = NativeTokenSymbol,
                            Amount = freeAmount
                        }
                    }
                },
                RefreshSeconds = refreshSeconds,
                Threshold = threshold
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = NativeTokenSymbol,
                    BasicFee = basicFee
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

        {
            var freeAllowances = await TokenContractStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
            if (threshold <= initialBalance)
            {
                freeAllowances.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
                freeAllowances.Value.First().Amount.ShouldBe(freeAmount);
            }
            else
            {
                freeAllowances.Value.ShouldBeEmpty();
            }
        }

        var sizeFeeSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = NativeTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        {
            var freeAllowances = await TokenContractStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
            if (threshold <= initialBalance)
            {
                freeAllowances.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
                freeAllowances.Value.First().Amount.ShouldBe(newFreeAllowance);
            }
            else
            {
                freeAllowances.Value.ShouldBeEmpty();
            }
        }

        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = NativeTokenSymbol,
                Owner = DefaultSender
            });
            balance.Balance.ShouldBe(afterBalance);
        }
    }

    [Theory]
    [InlineData(10000, 10000, 1000, 1000, 200, 200, 100, 100, 100, 100, 1000, 1000)]
    [InlineData(10000, 10000, 1000, 1000, 100, 100, 200, 200, 0, 0, 900, 900)]
    [InlineData(10000, 10000, 1000, 1000, 0, 100, 200, 100, 0, 0, 800, 1000)]
    [InlineData(10000, 10000, 0, 0, 100, 100, 100, 100, 0, 0, 0, 0)]
    public async Task FreeAllowances_MultToken_Test(
        long threshold, long initialBalance,
        long baseFeeBalance, long sizeFeeBalance,
        long baseFeeFreeAmount, long sizeFeeFreeAmount, long basicFee, long sizeFee,
        long newBaseFreeAllowance, long newSizeFreeAllowance, long afterBaseFeeBalance, long afterSizeFeeBalance)
    {
        var basicFeeSymbol = "BASIC";
        var sizeFeeSymbol = "SIZE";

        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, basicFeeSymbol);
        await CreateTokenAsync(DefaultSender, sizeFeeSymbol);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);

        if (baseFeeBalance != 0 && sizeFeeBalance != 0)
        {
            await IssueTokenToDefaultSenderAsync(basicFeeSymbol, baseFeeBalance);
            await IssueTokenToDefaultSenderAsync(sizeFeeSymbol, sizeFeeBalance);
        }

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractStub.ConfigMethodFeeFreeAllowances), new MethodFeeFreeAllowancesConfig
            {
                FreeAllowances = new MethodFeeFreeAllowances
                {
                    Value =
                    {
                        new MethodFeeFreeAllowance
                        {
                            Symbol = basicFeeSymbol,
                            Amount = baseFeeFreeAmount
                        },
                        new MethodFeeFreeAllowance
                        {
                            Symbol = sizeFeeSymbol,
                            Amount = sizeFeeFreeAmount
                        }
                    }
                },
                RefreshSeconds = 100,
                Threshold = threshold
            });


        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = basicFeeSymbol,
                    BasicFee = basicFee
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

        var sizeFeeSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = sizeFeeSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = NativeTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

        {
            var freeAllowances = await TokenContractStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
            if (threshold <= initialBalance)
            {
                freeAllowances.Value.First().Symbol.ShouldBe(basicFeeSymbol);
                freeAllowances.Value.First().Amount.ShouldBe(baseFeeFreeAmount);
                freeAllowances.Value.Last().Symbol.ShouldBe(sizeFeeSymbol);
                freeAllowances.Value.Last().Amount.ShouldBe(sizeFeeFreeAmount);
            }
            else
            {
                freeAllowances.Value.ShouldBeEmpty();
            }
        }

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        {
            var freeAllowances = await TokenContractStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
            if (threshold <= initialBalance)
            {
                freeAllowances.Value.First().Symbol.ShouldBe(basicFeeSymbol);
                freeAllowances.Value.First().Amount.ShouldBe(newBaseFreeAllowance);
                freeAllowances.Value.Last().Symbol.ShouldBe(sizeFeeSymbol);
                freeAllowances.Value.Last().Amount.ShouldBe(newSizeFreeAllowance);
            }
            else
            {
                freeAllowances.Value.ShouldBeEmpty();
            }
        }

        var baseFeeSymbolBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = basicFeeSymbol,
            Owner = DefaultSender
        });
        baseFeeSymbolBalance.Balance.ShouldBe(afterBaseFeeBalance);

        var sizeFeeSymbolBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = sizeFeeSymbol,
            Owner = DefaultSender
        });
        sizeFeeSymbolBalance.Balance.ShouldBe(afterSizeFeeBalance);
    }

    [Theory]
    [InlineData(1000, 1000, 1000, 1000, 1000, 1000, 1000, 50, 50, 100, 100, 50, 50, 10, 10, 80, 80, 50, 30, 920)]
    public async Task ChargeTransactionFee_Delegate(
        long threshold, long initialBalance, long initialDelegateeBalance, long initialUserBalance,
        long delegateeAmountNativeToken, long delegateeAmountBasic, long delegateeAmountSize,
        long baseFeeBalance, long sizeFeeBalance, long baseFeeDelegateBalance, long sizeFeeDelegateBalance,
        long baseFeeUserBalance, long sizeFeeUserBalance, long baseFeeFreeAmount, long sizeFeeFreeAmount,
        long basicFee, long sizeFee, long afterBalanceDefault, long afterBalanceDelegatee, long afterDelegateeAmount
    )
    {
        var basicFeeSymbol = "BASIC";
        var sizeFeeSymbol = "SIZE";

        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, basicFeeSymbol);
        await CreateTokenAsync(DefaultSender, sizeFeeSymbol);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);
        await IssueTokenToUserAsync(NativeTokenSymbol, initialDelegateeBalance, delegateeAddress);
        await IssueTokenToUserAsync(NativeTokenSymbol, initialUserBalance, userAddress);

        var delegations = new Dictionary<string, long>
        {
            [NativeTokenSymbol] = delegateeAmountNativeToken,
            [basicFeeSymbol] = delegateeAmountBasic,
            [sizeFeeSymbol] = delegateeAmountSize
        };
        var transactionResult = await TokenContractStub2.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput
        {
            DelegatorAddress = DefaultSender,
            Delegations =
            {
                delegations
            }
        });
        // Test Case 11
        {
            var result = await TokenContractStubA.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegateeAddress,
                    DelegatorAddress = DefaultSender
                });
            result.BlockHeight.ShouldBe(transactionResult.TransactionResult.BlockNumber);
        }
        {
            var result = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegateeAddress,
                    DelegatorAddress = DefaultSender
                });
            result.Delegations[NativeTokenSymbol].ShouldBe(delegateeAmountNativeToken);
        }

        if (baseFeeBalance != 0 && sizeFeeBalance != 0 &&
            baseFeeDelegateBalance != 0 && sizeFeeDelegateBalance != 0 &&
            baseFeeUserBalance != 0 && sizeFeeUserBalance != 0)
        {
            await IssueTokenToDefaultSenderAsync(basicFeeSymbol, baseFeeBalance);
            await IssueTokenToDefaultSenderAsync(sizeFeeSymbol, sizeFeeBalance);
            await IssueTokenToUserAsync(basicFeeSymbol, baseFeeDelegateBalance, delegateeAddress);
            await IssueTokenToUserAsync(sizeFeeSymbol, sizeFeeDelegateBalance, delegateeAddress);
            await IssueTokenToUserAsync(basicFeeSymbol, baseFeeUserBalance, userAddress);
            await IssueTokenToUserAsync(sizeFeeSymbol, sizeFeeUserBalance, userAddress);
        }

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractStub.ConfigMethodFeeFreeAllowances), new MethodFeeFreeAllowancesConfig
            {
                FreeAllowances = new MethodFeeFreeAllowances
                {
                    Value =
                    {
                        new MethodFeeFreeAllowance
                        {
                            Symbol = basicFeeSymbol,
                            Amount = baseFeeFreeAmount
                        },
                        new MethodFeeFreeAllowance
                        {
                            Symbol = sizeFeeSymbol,
                            Amount = sizeFeeFreeAmount
                        }
                    }
                },
                RefreshSeconds = 100,
                Threshold = threshold
            });
        {
            var freeAllowances = await TokenContractStub.GetMethodFeeFreeAllowances.CallAsync(userAddress);
            if (threshold <= initialBalance)
            {
                freeAllowances.Value.First().Symbol.ShouldBe(basicFeeSymbol);
                freeAllowances.Value.First().Amount.ShouldBe(baseFeeFreeAmount);
                freeAllowances.Value.Last().Symbol.ShouldBe(sizeFeeSymbol);
                freeAllowances.Value.Last().Amount.ShouldBe(sizeFeeFreeAmount);
            }
            else
            {
                freeAllowances.Value.ShouldBeEmpty();
            }
        }

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = basicFeeSymbol,
                    BasicFee = basicFee
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

        var sizeFeeSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = sizeFeeSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = NativeTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);


        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);
        {
            var chargeFeeRetUser = await TokenContractStub3.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            chargeFeeRetUser.Output.Success.ShouldBe(false);
            chargeFeeRetUser.Output.ChargingInformation.ShouldBe("Transaction fee not enough.");

            var afterBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = basicFeeSymbol,
                Owner = userAddress
            });
            afterBalance.Balance.ShouldBe(0);
        }
        // Test Case 12
        {
            var delegationResult = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegateeAddress,
                    DelegatorAddress = DefaultSender
                });
            var chargeFeeRetDefault =
                await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            if (chargeFeeRetDefault.Transaction.RefBlockNumber >= delegationResult.BlockHeight + 2)
            {
                chargeFeeRetDefault.Output.Success.ShouldBe(true);
                
                var afterBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = basicFeeSymbol,
                    Owner = DefaultSender
                });
                afterBalance.Balance.ShouldBe(afterBalanceDefault);

                var afterDelegateeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = basicFeeSymbol,
                    Owner = delegateeAddress
                });
                afterDelegateeBalance.Balance.ShouldBe(afterBalanceDelegatee);
                var delegation = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                    new GetTransactionFeeDelegationsOfADelegateeInput
                    {
                        DelegateeAddress = delegateeAddress,
                        DelegatorAddress = DefaultSender
                    });
                delegation.Delegations[basicFeeSymbol].ShouldBe(afterDelegateeAmount);
            }
            else
            {
                chargeFeeRetDefault.Output.Success.ShouldBe(false);
                
                var delegation = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                    new GetTransactionFeeDelegationsOfADelegateeInput
                    {
                        DelegateeAddress = delegateeAddress,
                        DelegatorAddress = DefaultSender
                    });
                delegation.Delegations[basicFeeSymbol].ShouldBe(delegateeAmountBasic);
            }
        }
    }

    [Theory]
    [InlineData(1000, 1000, 1000, 10, 10, 10, 50, 50, 100, 100, 20, 20, 80, 80, 100, 10)]
    [InlineData(1000, 1000, 1000, 1000, 1000, 1000, 50, 50, 30, 30, 20, 20, 80, 80, 30, 1000)]
    [InlineData(1000, 1000, 1000, 1000, 0, 1000, 50, 50, 100, 100, 20, 20, 80, 80, 100, 1000)]
    public async Task ChargeTransactionFee_Delegate_Failed(
        long threshold, long initialBalance, long initialDelegateeBalance,
        long delegateeAmountNativeToken, long delegateeAmountBasic, long delegateeAmountSize,
        long baseFeeBalance, long sizeFeeBalance, long baseFeeDelegateBalance, long sizeFeeDelegateBalance,
        long baseFeeFreeAmount, long sizeFeeFreeAmount, long basicFee, long sizeFee,
        long afterBalanceDelegatee, long afterDelegateeAmount
    )
    {
        var basicFeeSymbol = "BASIC";
        var sizeFeeSymbol = "SIZE";

        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, basicFeeSymbol);
        await CreateTokenAsync(DefaultSender, sizeFeeSymbol);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);
        await IssueTokenToUserAsync(NativeTokenSymbol, initialDelegateeBalance, delegateeAddress);

        var delegations = new Dictionary<string, long>
        {
            [NativeTokenSymbol] = delegateeAmountNativeToken,
            [basicFeeSymbol] = delegateeAmountBasic,
            [sizeFeeSymbol] = delegateeAmountSize
        };
        await TokenContractStub2.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput
        {
            DelegatorAddress = DefaultSender,
            Delegations =
            {
                delegations
            }
        });
        {
            var result = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegateeAddress,
                    DelegatorAddress = DefaultSender
                });
            result.Delegations[NativeTokenSymbol].ShouldBe(delegateeAmountNativeToken);
        }

        if (baseFeeBalance != 0 && sizeFeeBalance != 0 &&
            baseFeeDelegateBalance != 0 && sizeFeeDelegateBalance != 0)
        {
            await IssueTokenToDefaultSenderAsync(basicFeeSymbol, baseFeeBalance);
            await IssueTokenToDefaultSenderAsync(sizeFeeSymbol, sizeFeeBalance);
            await IssueTokenToUserAsync(basicFeeSymbol, baseFeeDelegateBalance, delegateeAddress);
            await IssueTokenToUserAsync(sizeFeeSymbol, sizeFeeDelegateBalance, delegateeAddress);
        }

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractStub.ConfigMethodFeeFreeAllowances), new MethodFeeFreeAllowancesConfig
            {
                FreeAllowances = new MethodFeeFreeAllowances
                {
                    Value =
                    {
                        new MethodFeeFreeAllowance
                        {
                            Symbol = basicFeeSymbol,
                            Amount = baseFeeFreeAmount
                        },
                        new MethodFeeFreeAllowance
                        {
                            Symbol = sizeFeeSymbol,
                            Amount = sizeFeeFreeAmount
                        }
                    }
                },
                RefreshSeconds = 100,
                Threshold = threshold
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = basicFeeSymbol,
                    BasicFee = basicFee
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

        var sizeFeeSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = sizeFeeSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = NativeTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);


        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        {
            var chargeFeeRetUser = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            chargeFeeRetUser.Output.Success.ShouldBe(false);
            chargeFeeRetUser.Output.ChargingInformation.ShouldBe("Transaction fee not enough.");

            var afterBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = basicFeeSymbol,
                Owner = DefaultSender
            });
            afterBalance.Balance.ShouldBe(0);
        }
        {
            var afterDelegateeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = basicFeeSymbol,
                Owner = delegateeAddress
            });
            afterDelegateeBalance.Balance.ShouldBe(afterBalanceDelegatee);
            var delegation = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegateeAddress,
                    DelegatorAddress = DefaultSender
                });
            delegation.Delegations[sizeFeeSymbol].ShouldBe(afterDelegateeAmount);
        }
    }

    [Theory]
    [InlineData(1000, 1000, 100, 100, 1000, 1000)]
    public async Task ChargeTransactionFee_Delegate_DefaultSizeFee(
        long initialDelegateeBalance, long delegateeAmountBasic,
        long basicFee, long sizeFee, long exceptDelegateeBalance, long exceptDelegateeElfBalance)
    {
        var basicFeeSymbol = "BASIC";
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, basicFeeSymbol);

        await IssueTokenToUserAsync(NativeTokenSymbol, initialDelegateeBalance, delegateeAddress);
        await IssueTokenToUserAsync(basicFeeSymbol, initialDelegateeBalance, delegateeAddress);

        var delegations = new Dictionary<string, long>
        {
            [basicFeeSymbol] = delegateeAmountBasic,
        };
        await TokenContractStub2.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput
        {
            DelegatorAddress = userAddress,
            Delegations =
            {
                delegations
            }
        });

        {
            var result = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegateeAddress,
                    DelegatorAddress = userAddress
                });
            result.Delegations[basicFeeSymbol].ShouldBe(delegateeAmountBasic);
        }

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = basicFeeSymbol,
                    BasicFee = basicFee
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        {
            var chargeFeeRetUser = await TokenContractStub3.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            chargeFeeRetUser.Output.Success.ShouldBe(false);
            chargeFeeRetUser.Output.ChargingInformation.ShouldBe("Transaction fee not enough.");

            var afterBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = basicFeeSymbol,
                Owner = userAddress
            });
            afterBalance.Balance.ShouldBe(0);

            var afterDelegateeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = basicFeeSymbol,
                Owner = delegateeAddress
            });
            afterDelegateeBalance.Balance.ShouldBe(exceptDelegateeBalance);

            var afterDelegateeElfBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = NativeTokenSymbol,
                Owner = delegateeAddress
            });
            afterDelegateeElfBalance.Balance.ShouldBe(exceptDelegateeElfBalance);
        }
    }

    private async Task<long> GetTokenSupplyAmount(string tokenSymbol)
    {
        var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = tokenSymbol
        });
        return tokenInfo.Supply;
    }

    private async Task<List<long>> GetDefaultBalancesAsync(string[] tokenSymbolList)
    {
        var balances = new List<long>();
        foreach (var symbol in tokenSymbolList)
            balances.Add(await GetBalanceAsync(DefaultSender, symbol));
        return balances;
    }

    private async Task CreateTokenAsync(Address creator, string tokenSymbol, bool isBurned = true)
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = tokenSymbol,
            TokenName = tokenSymbol + " name",
            TotalSupply = 1000_00000000,
            IsBurnable = isBurned,
            Issuer = creator,
        });
    }

    private async Task IssueTokenToDefaultSenderAsync(string tokenSymbol, long amount)
    {
        var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
        {
            Symbol = tokenSymbol,
            Amount = amount,
            To = DefaultSender,
        });
        issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    private async Task IssueTokenToUserAsync(string tokenSymbol, long amount, Address to)
    {
        var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
        {
            Symbol = tokenSymbol,
            Amount = amount,
            To = to,
        });
        issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    // single node
    private async Task SubmitAndPassProposalOfDefaultParliamentAsync(Address contractAddress, string methodName,
        IMessage input)
    {
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = defaultParliament,
            ToAddress = contractAddress,
            Params = input.ToByteString(),
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var createProposalRet = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        createProposalRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposalId = createProposalRet.Output;
        await ParliamentContractStub.Approve.SendAsync(proposalId);
        var releaseRet = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    private async Task<long> GetBalanceAsync(Address address, string tokenSymbol)
    {
        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = tokenSymbol,
            Owner = address
        });
        return balance.Balance;
    }
}