using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

public partial class ExecutePluginTransactionDirectlyTest
{
    [Fact]
    public async Task ConfigMethodFeeFreeAllowances_Test()
    {
        await SetPrimaryTokenSymbolAsync();

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 0
                                }
                            }
                        },
                        RefreshSeconds = 600,
                        Threshold = 0
                    }
                }
            });

        {
            var config = await TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
            config.Value.Count.ShouldBe(1);
            config.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().Threshold.ShouldBe(0);
            config.Value.First().RefreshSeconds.ShouldBe(600);
            config.Value.First().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Amount.ShouldBe(0);

            var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
            freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(0);
        }

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, 2_00000000);
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        RefreshSeconds = 600,
                        Threshold = 1_00000000
                    }
                }
            });

        {
            var config = await TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
            config.Value.Count.ShouldBe(1);
            config.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().Threshold.ShouldBe(1_00000000);
            config.Value.First().RefreshSeconds.ShouldBe(600);
            config.Value.First().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

            var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
            freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
        }
    }

    [Fact]
    public async Task ConfigMethodFeeFreeAllowances_Unauthorized_Test()
    {
        var result =
            await TokenContractImplStub.ConfigMethodFeeFreeAllowances.SendWithExceptionAsync(
                new ConfigMethodFeeFreeAllowancesInput());
        result.TransactionResult.Error.ShouldContain("Unauthorized behavior.");
    }

    [Fact]
    public async Task ConfigMethodFeeFreeAllowances_MultipleTokens_OneByOne_Test()
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAndIssueAsync();

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        RefreshSeconds = 600,
                        Threshold = 1_00000000
                    }
                }
            });

        {
            var config = await TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
            config.Value.Count.ShouldBe(1);
            config.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().Threshold.ShouldBe(1_00000000);
            config.Value.First().RefreshSeconds.ShouldBe(600);
            config.Value.First().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

            var userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
            userAFreeAllowances.Result.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            userAFreeAllowances.Result.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
            userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
            var userBFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserBAddress);
            userBFreeAllowances.Result.Map.Count.ShouldBe(0);
            var userCFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserCAddress);
            userCFreeAllowances.Result.Map.ShouldBe(userAFreeAllowances.Result.Map);
        }

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        RefreshSeconds = 300,
                        Threshold = 1_000000
                    }
                }
            });

        {
            var config = await TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
            config.Value.Count.ShouldBe(2);
            config.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().Threshold.ShouldBe(1_00000000);
            config.Value.First().RefreshSeconds.ShouldBe(600);
            config.Value.First().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.First().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

            config.Value.Last().Symbol.ShouldBe(USDT);
            config.Value.Last().Threshold.ShouldBe(1_000000);
            config.Value.Last().RefreshSeconds.ShouldBe(300);
            config.Value.Last().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            config.Value.Last().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            config.Value.Last().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

            var userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
            userAFreeAllowances.Result.Map.Count.ShouldBe(1);
            userAFreeAllowances.Result.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            userAFreeAllowances.Result.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
            userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
            var userBFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserBAddress);
            userBFreeAllowances.Result.Map.Count.ShouldBe(1);
            userBFreeAllowances.Result.Map.Keys.First().ShouldBe(USDT);
            userBFreeAllowances.Result.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
            userBFreeAllowances.Result.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
            userBFreeAllowances.Result.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
            var userCFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserCAddress);
            userAFreeAllowances.Result.Map.Add(USDT, userBFreeAllowances.Result.Map.Values.First());
            userCFreeAllowances.Result.Map.ShouldBe(userAFreeAllowances.Result.Map);
        }
    }

    [Fact]
    public async Task ConfigMethodFeeFreeAllowances_MultipleTokens_AtOnce_Test()
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAndIssueAsync();

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        RefreshSeconds = 600,
                        Threshold = 1_00000000
                    },
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        RefreshSeconds = 300,
                        Threshold = 1_000000
                    }
                }
            });

        var config = await TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Value.Count.ShouldBe(2);
        config.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
        config.Value.First().Threshold.ShouldBe(1_00000000);
        config.Value.First().RefreshSeconds.ShouldBe(600);
        config.Value.First().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        config.Value.First().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        config.Value.First().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

        config.Value.Last().Symbol.ShouldBe(USDT);
        config.Value.Last().Threshold.ShouldBe(1_000000);
        config.Value.Last().RefreshSeconds.ShouldBe(300);
        config.Value.Last().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        config.Value.Last().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        config.Value.Last().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

        var userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(1);
        userAFreeAllowances.Result.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        userAFreeAllowances.Result.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
        var userBFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserBAddress);
        userBFreeAllowances.Result.Map.Count.ShouldBe(1);
        userBFreeAllowances.Result.Map.Keys.First().ShouldBe(USDT);
        userBFreeAllowances.Result.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        userBFreeAllowances.Result.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        userBFreeAllowances.Result.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
        var userCFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserCAddress);
        userAFreeAllowances.Result.Map.Add(USDT, userBFreeAllowances.Result.Map.Values.First());
        userCFreeAllowances.Result.Map.ShouldBe(userAFreeAllowances.Result.Map);
    }

    [Fact]
    public async Task ConfigMethodFeeFreeAllowances_MultipleTokens_Modify_Test()
    {
        await ConfigMethodFeeFreeAllowances_MultipleTokens_AtOnce_Test();
        await CreateTokenAsync(DefaultSender, "ABC");

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 2_00000000
                                },
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = "ABC",
                                    Amount = 2_00000000
                                }
                            }
                        },
                        RefreshSeconds = 1200,
                        Threshold = 2_000000
                    }
                }
            });

        var config = await TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Value.Count.ShouldBe(2);
        config.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
        config.Value.First().Threshold.ShouldBe(1_00000000);
        config.Value.First().RefreshSeconds.ShouldBe(600);
        config.Value.First().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        config.Value.First().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        config.Value.First().FreeAllowances.Map.Values.First().Amount.ShouldBe(1_00000000);

        config.Value.Last().Symbol.ShouldBe(USDT);
        config.Value.Last().Threshold.ShouldBe(2_000000);
        config.Value.Last().RefreshSeconds.ShouldBe(1200);
        config.Value.Last().FreeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        config.Value.Last().FreeAllowances.Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        config.Value.Last().FreeAllowances.Map.Values.First().Amount.ShouldBe(2_00000000);
        config.Value.Last().FreeAllowances.Map.Keys.Last().ShouldBe("ABC");
        config.Value.Last().FreeAllowances.Map.Values.Last().Symbol.ShouldBe("ABC");
        config.Value.Last().FreeAllowances.Map.Values.Last().Amount.ShouldBe(2_00000000);

        var userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(1);
        userAFreeAllowances.Result.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        userAFreeAllowances.Result.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        userAFreeAllowances.Result.Map.Values.First().Map.Values.First().Amount.ShouldBe(1_00000000);
        var userBFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserBAddress);
        userBFreeAllowances.Result.Map.Count.ShouldBe(0);
        var userCFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserCAddress);
        userCFreeAllowances.Result.Map.ShouldBe(userAFreeAllowances.Result.Map);
    }

    [Fact]
    public async Task ConfigMethodFeeFreeAllowances_InvalidInput_Test()
    {
        var message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance()
                }
            });
        message.ShouldContain("Invalid input symbol");

        message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = "TEST"
                    }
                }
            });
        message.ShouldContain("Symbol TEST not exist");

        message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol
                    }
                }
            });
        message.ShouldContain("Invalid input allowances");

        message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        Threshold = -1
                    }
                }
            });
        message.ShouldContain("Invalid input threshold");

        message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = 1_00000000
                                }
                            }
                        },
                        Threshold = 1_00000000,
                        RefreshSeconds = -1
                    }
                }
            });
        message.ShouldContain("Invalid input refresh seconds");
    }

    [Fact]
    public async Task RemoveMethodFeeFreeAllowancesConfig_Unauthorized_Test()
    {
        var result =
            await TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig.SendWithExceptionAsync(
                new RemoveMethodFeeFreeAllowancesConfigInput());
        result.TransactionResult.Error.ShouldContain("Unauthorized behavior.");
    }

    [Fact]
    public async Task RemoveMethodFeeFreeAllowancesConfig_Test()
    {
        await ConfigMethodFeeFreeAllowances_MultipleTokens_AtOnce_Test();
        await IssueTokenToUserAsync(USDT, 1_000000, UserAAddress);

        var userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(2);
        var config = TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Result.Value.Count.ShouldBe(2);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig),
            new RemoveMethodFeeFreeAllowancesConfigInput
            {
                Symbols = { USDT }
            });
        config = TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Result.Value.Count.ShouldBe(1);
        config.Result.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
        userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(1);
        userAFreeAllowances.Result.Map.Keys.First().ShouldBe(NativeTokenSymbol);

        // symbol not exist
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig),
            new RemoveMethodFeeFreeAllowancesConfigInput
            {
                Symbols = { USDT }
            });
        config = TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Result.Value.Count.ShouldBe(1);
        config.Result.Value.First().Symbol.ShouldBe(NativeTokenSymbol);
        userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(1);
        userAFreeAllowances.Result.Map.Keys.First().ShouldBe(NativeTokenSymbol);

        // Duplicate symbols
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig),
            new RemoveMethodFeeFreeAllowancesConfigInput
            {
                Symbols = { "ELF", "ELF" }
            });
        config = TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Result.Value.Count.ShouldBe(0);
        userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(0);
    }

    [Fact]
    public async Task RemoveMethodFeeFreeAllowancesConfig_MultipleTokens_AtOnce_Test()
    {
        await ConfigMethodFeeFreeAllowances_MultipleTokens_AtOnce_Test();
        await IssueTokenToUserAsync(USDT, 1_000000, UserAAddress);

        var userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(2);
        var config = TokenContractImplStub.GetMethodFeeFreeAllowancesConfig.CallAsync(new Empty());
        config.Result.Value.Count.ShouldBe(2);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig),
            new RemoveMethodFeeFreeAllowancesConfigInput
            {
                Symbols = { "ELF", USDT }
            });
        userAFreeAllowances = TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(UserAAddress);
        userAFreeAllowances.Result.Map.Count.ShouldBe(0);
    }

    [Fact]
    public async Task RemoveMethodFeeFreeAllowancesConfig_InvalidInput_Test()
    {
        var message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig),
            new RemoveMethodFeeFreeAllowancesConfigInput
            {
                Symbols = { USDT }
            });
        message.ShouldContain("Method fee free allowances config not set");

        await ConfigMethodFeeFreeAllowances_MultipleTokens_AtOnce_Test();
        message = await SubmitProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.RemoveMethodFeeFreeAllowancesConfig),
            new RemoveMethodFeeFreeAllowancesConfigInput());
        message.ShouldContain("Invalid input");
    }

    [Theory]
    [InlineData(1000, 0, 1000, 0, 100, 0, 0, 0, 800, 0, 1000, 200)]
    [InlineData(1000, 0, 100, 0, 100, 0, 0, 0, 0, 0, 900, 200)]
    [InlineData(1000, 1000, 500, 10, 100, 500, 20, 100, 300, 500, 1000, 200)]
    [InlineData(1000, 1000, 150, 10, 100, 150, 20, 100, 0, 100, 1000, 200)]
    public async Task ChargeTransactionFee_Test(long initialELFBalance, long initialUSDTBalance,
        long freeAmountELF, long refreshSecondsELF, long thresholdELF, long freeAmountUSDT, long refreshSecondsUSDT,
        long thresholdUSDT, long newFreeAmountELF, long newFreeAmountUSDT, long afterBalance, long basicFee)
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, USDT);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialELFBalance);
        await IssueTokenToDefaultSenderAsync(USDT, initialUSDTBalance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = freeAmountELF
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsELF,
                        Threshold = thresholdELF
                    },
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = NativeTokenSymbol,
                                    Amount = freeAmountUSDT
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsUSDT,
                        Threshold = thresholdUSDT
                    }
                }
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

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(freeAmountELF);
        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(freeAmountUSDT);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress
        };

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(newFreeAmountELF);
        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(newFreeAmountUSDT);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, afterBalance);
    }

    [Theory]
    // case 21
    [InlineData(1000, 1000, 1000, 1000, 1000, 10, 100, 1000, 20, 100, 700, 500, 1000, 1000, 1000, 1000, 300, 500)]
    // case 22
    [InlineData(1000, 1000, 1000, 1000, 100, 10, 100, 1000, 20, 100, 0, 500, 100, 1000, 1000, 1000, 1000, 500)]
    // case 25
    [InlineData(1000, 100, 1000, 1000, 1000, 10, 10000, 1000, 20, 100, 1000, 500, 0, 1000, 1000, 100, 1000, 500)]
    public async Task ChargeTransactionFee_MultipleTokens_Test(long initialELFBalance, long initialUSDTBalance,
        long initialToken1Balance, long initialToken2Balance, long freeAmountELF, long refreshSecondsELF,
        long thresholdELF, long freeAmountUSDT, long refreshSecondsUSDT, long thresholdUSDT, long newFreeAmountELF,
        long newFreeAmountUSDT, long afterToken1Balance, long afterToken2Balance, long afterELFBalance,
        long afterUSDTBalance, long sizeFee, long basicFee)
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, USDT);

        await CreateTokenAsync(DefaultSender, Token1);
        await CreateTokenAsync(DefaultSender, Token2);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialELFBalance);
        await IssueTokenToDefaultSenderAsync(USDT, initialUSDTBalance);
        await IssueTokenToDefaultSenderAsync(Token1, initialToken1Balance);
        await IssueTokenToDefaultSenderAsync(Token2, initialToken2Balance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token1,
                                    Amount = freeAmountELF
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsELF,
                        Threshold = thresholdELF
                    },
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token2,
                                    Amount = freeAmountUSDT
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsUSDT,
                        Threshold = thresholdUSDT
                    }
                }
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = Token2,
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
                    TokenSymbol = Token1,
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

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        if (initialELFBalance >= thresholdELF)
        {
            freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
            freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
            freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(freeAmountELF);
        }

        if (initialUSDTBalance >= thresholdUSDT)
        {
            freeAllowances.Map.Keys.Last().ShouldBe(USDT);
            freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
            freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
            freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(freeAmountUSDT);
        }

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(initialToken1Balance + freeAmountELF >= sizeFee);


        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        if (initialELFBalance >= thresholdELF)
        {
            freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
            freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
            freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(newFreeAmountELF);
        }

        if (initialUSDTBalance >= thresholdUSDT)
        {
            freeAllowances.Map.Keys.Last().ShouldBe(USDT);
            freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
            freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
            freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(newFreeAmountUSDT);
        }

        await CheckDefaultSenderTokenAsync(Token1, afterToken1Balance);
        await CheckDefaultSenderTokenAsync(Token2, afterToken2Balance);
        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, afterELFBalance);
        await CheckDefaultSenderTokenAsync(USDT, afterUSDTBalance);
    }

    [Theory]
    [InlineData(1000, 600, 200, 200)]
    public async Task ChargeTransactionFee_NoFreeAllowance_Test(long initialBalance, long afterBalance, long sizeFee,
        long basicFee)
    {
        await SetPrimaryTokenSymbolAsync();

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);

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

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, afterBalance);
    }

    [Theory]
    [InlineData(1000, 1000, 1000, 2000, 2000, 0, 1000, 1000, 500, 1000, 1000, 1000, 1500)]
    public async Task ChargeTransactionFee_SingleThreshold_Test(long initialBalance, long initialToken1Balance,
        long initialToken2Balance, long freeAmountTokenA, long freeAmountTokenB, long refreshSeconds,
        long threshold, long newFreeAmountTokenA, long newFreeAmountTokenB, long afterToken1Balance,
        long afterToken2Balance, long sizeFee, long basicFee)
    {
        await SetPrimaryTokenSymbolAsync();

        await CreateTokenAsync(DefaultSender, Token1);
        await CreateTokenAsync(DefaultSender, Token2);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);
        await IssueTokenToDefaultSenderAsync(Token1, initialToken1Balance);
        await IssueTokenToDefaultSenderAsync(Token2, initialToken2Balance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token1,
                                    Amount = freeAmountTokenA
                                },
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token2,
                                    Amount = freeAmountTokenB
                                }
                            }
                        },
                        RefreshSeconds = refreshSeconds,
                        Threshold = threshold
                    }
                }
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = Token2,
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
                    TokenSymbol = Token1,
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

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);

        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(freeAmountTokenA);

        freeAllowances.Map.Values.First().Map.Keys.Last().ShouldBe(Token2);
        freeAllowances.Map.Values.First().Map.Values.Last().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.First().Map.Values.Last().Amount.ShouldBe(freeAmountTokenB);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);


        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);

        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(newFreeAmountTokenA);

        freeAllowances.Map.Values.First().Map.Keys.Last().ShouldBe(Token2);
        freeAllowances.Map.Values.First().Map.Values.Last().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.First().Map.Values.Last().Amount.ShouldBe(newFreeAmountTokenB);

        await CheckDefaultSenderTokenAsync(Token1, afterToken1Balance);
        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, afterToken2Balance);
    }

    [Theory]
    [InlineData(1000, 1000, 1000, 1000, 1000, 10, 100, 1000, 20, 100, 0, 1000, 0, 1000, 3000, 5000)]
    public async Task ChargeTransactionFee_NotEnough_Test(long initialELFBalance, long initialUSDTBalance,
        long initialToken1Balance, long initialToken2Balance, long freeAmountELF, long refreshSecondsELF,
        long thresholdELF, long freeAmountUSDT, long refreshSecondsUSDT, long thresholdUSDT, long newFreeAmountELF,
        long newFreeAmountUSDT, long afterToken1Balance, long afterToken2Balance, long sizeFee, long basicFee)
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, USDT);

        await CreateTokenAsync(DefaultSender, Token1);
        await CreateTokenAsync(DefaultSender, Token2);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialELFBalance);
        await IssueTokenToDefaultSenderAsync(USDT, initialUSDTBalance);
        await IssueTokenToDefaultSenderAsync(Token1, initialToken1Balance);
        await IssueTokenToDefaultSenderAsync(Token2, initialToken2Balance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token1,
                                    Amount = freeAmountELF
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsELF,
                        Threshold = thresholdELF
                    },
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token2,
                                    Amount = freeAmountUSDT
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsUSDT,
                        Threshold = thresholdUSDT
                    }
                }
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = Token1,
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
                    TokenSymbol = Token1,
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

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(freeAmountELF);
        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(freeAmountUSDT);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(false);


        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);

        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(newFreeAmountUSDT);

        await CheckDefaultSenderTokenAsync(Token1, afterToken1Balance);
        await CheckDefaultSenderTokenAsync(Token2, afterToken2Balance);
    }

    [Theory]
    [InlineData(10000, 500, 50, 10000, 300, 100, 100, 100, 2000, 10000, 7800, 10000)]
    public async Task ChargeTransactionFee_ClearFreeAllowance_Test(long initialBalance, long freeAmount,
        long refreshSeconds, long threshold, long firstFreeAmount, long secondFreeAmount, long sizeFee, long basicFee,
        long transferAmount, long firstBalance, long secondBalance, long thirdBalance)
    {
        await SetPrimaryTokenSymbolAsync();

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
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
                    }
                }
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

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(freeAmount);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(firstFreeAmount);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, firstBalance);

        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = transferAmount,
            Symbol = NativeTokenSymbol,
            To = UserCAddress,
            Memo = "test"
        });

        chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, secondBalance);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.ShouldBeEmpty();

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, transferAmount + sizeFee + basicFee);

        chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, thirdBalance);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(secondFreeAmount);
    }

    [Fact]
    public async Task ChargeTransactionFee_RefreshTime_Test()
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, USDT);

        await CreateTokenAsync(DefaultSender, Token1);
        await CreateTokenAsync(DefaultSender, Token2);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, 10000);
        await IssueTokenToDefaultSenderAsync(USDT, 10000);
        await IssueTokenToDefaultSenderAsync(Token1, 10000);
        await IssueTokenToDefaultSenderAsync(Token2, 10000);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token1,
                                    Amount = 1000
                                }
                            }
                        },
                        RefreshSeconds = 2,
                        Threshold = 100
                    },
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token2,
                                    Amount = 1000
                                }
                            }
                        },
                        RefreshSeconds = 1,
                        Threshold = 100
                    }
                }
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = Token2,
                    BasicFee = 500
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
                    TokenSymbol = Token1,
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

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(1000);

        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(1000);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = 1000
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(0);

        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(500);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, 10000);
        await CheckDefaultSenderTokenAsync(USDT, 10000);
        await CheckDefaultSenderTokenAsync(Token1, 10000);
        await CheckDefaultSenderTokenAsync(Token2, 10000);

        chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(0);

        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(0);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, 10000);
        await CheckDefaultSenderTokenAsync(USDT, 10000);
        await CheckDefaultSenderTokenAsync(Token1, 9000);
        await CheckDefaultSenderTokenAsync(Token2, 10000);

        _blockTimeProvider.SetBlockTime(TimestampHelper.GetUtcNow().AddSeconds(1));

        chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(0);

        freeAllowances.Map.Keys.Last().ShouldBe(USDT);
        freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(Token2);
        freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(500);

        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, 10000);
        await CheckDefaultSenderTokenAsync(USDT, 10000);
        await CheckDefaultSenderTokenAsync(Token1, 8000);
        await CheckDefaultSenderTokenAsync(Token2, 10000);
    }

    [Theory]
    [InlineData(1000, 1000, 1000, 0, 10, 800, 1000, 1000, 100, 100)]
    public async Task ChargeTransactionFee_FreeAllowanceFirst_Test(long initialBalance, long initialToken1Balance,
        long freeAmount, long refreshSeconds, long threshold, long newFreeAmount, long afterToken1Balance,
        long afterELFBalance, long sizeFee, long basicFee)
    {
        await SetPrimaryTokenSymbolAsync();

        await CreateTokenAsync(DefaultSender, Token1);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialBalance);
        await IssueTokenToDefaultSenderAsync(Token1, initialToken1Balance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = Token1,
                                    Amount = freeAmount
                                }
                            }
                        },
                        RefreshSeconds = refreshSeconds,
                        Threshold = threshold
                    }
                }
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
                },
                new MethodFee
                {
                    Symbol = Token1,
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
                    TokenSymbol = NativeTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = Token1,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(freeAmount);

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(true);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
        freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(Token1);
        freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(newFreeAmount);

        await CheckDefaultSenderTokenAsync(Token1, afterToken1Balance);
        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, afterELFBalance);
    }

    [Theory]
    // case 24
    [InlineData(1000, 1000, 1000, 1000, Token1, 1000, Token1, 1000, 600, 10, Token2, 1000, Token2, 1000, 300, 10, 1000, 1000, 1000, 1000, 0, 0, 1000, 1000, 5000, 1, 1, USDT, 3000, Token2, 3000, false)]
    // case 28
    [InlineData(10000, 10000, 1000, 0, Token1, 2000, NativeTokenSymbol, 2000, 600, 10000, Token1, 1000, USDT, 200, 300, 10000, 2000, 0, 0, 200, 10000, 10000, 1000, 0, 2000, 1, 2, Token1, 1000, USDT, 200, true)]
    // case 29
    [InlineData(10000, 10000, 1000, 0, Token1, 2000, NativeTokenSymbol, 1000, 600, 10000, Token1, 1000, USDT, 100, 300, 10000, 1000, 1000, 0, 100, 10000, 10000, 1000, 0, 2000, 1, 2, Token1, 1000, USDT, 200, true)]
    // case 30
    [InlineData(10000, 10000, 1000, 0, Token1, 1000, NativeTokenSymbol, 1000, 600, 100, Token1, 1000, USDT, 100, 300, 100, 500, 0, 0, 100, 9000, 10000, 1000, 0, 2000, 1, 1, Token1, 1500, USDT, 200, true)]
    // case 31
    [InlineData(10000, 10000, 10000, 0, Token1, 1000, NativeTokenSymbol, 1000, 600, 100, Token1, 1000, "ELF", 1000, 300, 100, 0, 0, 0, 0, 9000, 10000, 7000, 0, 3000, 5, 3, NativeTokenSymbol, 3000, Token1, 3000, true)]
    // case 32
    [InlineData(10000, 10000, 1000, 0, Token1, 1000, NativeTokenSymbol, 1000, 600, 100, Token1, 1000, "ELF", 1000, 300, 100, 0, 0, 0, 0, 0, 10000, 0, 0, 3000, 5, 3, NativeTokenSymbol, 15000, USDT, 20000, false)]
    // case 33
    [InlineData(5000, 10000, 0, 0, Token1, 1000, Token1, 1000, 600, 100, Token2, 1000, Token2, 1000, 300, 100, 0, 0, 1000, 1000, 0, 8000, 0, 0, 10000, 1, 5, USDT, 2000, "ELF", 0, false)]
    public async Task ChargeTransactionFee_MultipleTxFeeTokens_Test(long initialELFBalance, long initialUSDTBalance,
        long initialToken1Balance, long initialToken2Balance, string firstFreeSymbolELF, long firstFreeAmountELF,
        string secondFreeSymbolELF, long secondFreeAmountELF, long refreshSecondsELF, long thresholdELF,
        string firstFreeSymbolUSDT, long firstFreeAmountUSDT, string secondFreeSymbolUSDT, long secondFreeAmountUSDT,
        long refreshSecondsUSDT, long thresholdUSDT, long newFirstFreeAmountELF, long newSecondFreeAmountELF,
        long newFirstFreeAmountUSDT, long newSecondFreeAmountUSDT, long afterELFBalance, long afterUSDTBalance,
        long afterToken1Balance, long afterToken2Balance, long sizeFee, int addedTokenWeight, int baseTokenWeight,
        string firstBaseFeeSymbol, long firstBaseFeeAmount, string secondBaseFeeSymbol, long secondBaseFeeAmount,
        bool chargeFeeResult)
    {
        await SetPrimaryTokenSymbolAsync();
        await CreateTokenAsync(DefaultSender, USDT);

        await CreateTokenAsync(DefaultSender, Token1);
        await CreateTokenAsync(DefaultSender, Token2);

        await IssueTokenToDefaultSenderAsync(NativeTokenSymbol, initialELFBalance);
        await IssueTokenToDefaultSenderAsync(USDT, initialUSDTBalance);
        await IssueTokenToDefaultSenderAsync(Token1, initialToken1Balance);
        await IssueTokenToDefaultSenderAsync(Token2, initialToken2Balance);

        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplStub.ConfigMethodFeeFreeAllowances), new ConfigMethodFeeFreeAllowancesInput
            {
                Value =
                {
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = NativeTokenSymbol,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = firstFreeSymbolELF,
                                    Amount = firstFreeAmountELF
                                },
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = secondFreeSymbolELF,
                                    Amount = secondFreeAmountELF
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsELF,
                        Threshold = thresholdELF
                    },
                    new ConfigMethodFeeFreeAllowance
                    {
                        Symbol = USDT,
                        MethodFeeFreeAllowances = new MethodFeeFreeAllowances
                        {
                            Value =
                            {
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = firstFreeSymbolUSDT,
                                    Amount = firstFreeAmountUSDT
                                },
                                new MethodFeeFreeAllowance
                                {
                                    Symbol = secondFreeSymbolUSDT,
                                    Amount = secondFreeAmountUSDT
                                }
                            }
                        },
                        RefreshSeconds = refreshSecondsUSDT,
                        Threshold = thresholdUSDT
                    }
                }
            });

        var methodFee = new MethodFees
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            Fees =
            {
                new MethodFee
                {
                    Symbol = firstBaseFeeSymbol,
                    BasicFee = firstBaseFeeAmount
                }
            }
        };

        if (secondBaseFeeAmount > 0)
        {
            methodFee.Fees.Add(new MethodFee
            {
                Symbol = secondBaseFeeSymbol,
                BasicFee = secondBaseFeeAmount
            });
        }
        
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

        var sizeFeeSymbolList = new SymbolListToPayTxSizeFee
        {
            SymbolsToPayTxSizeFee =
            {
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = NativeTokenSymbol,
                    AddedTokenWeight = 1,
                    BaseTokenWeight = 1
                },
                new SymbolToPayTxSizeFee
                {
                    TokenSymbol = Token1,
                    AddedTokenWeight = addedTokenWeight,
                    BaseTokenWeight = baseTokenWeight
                }
            }
        };
        await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
            nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

        var freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        if (initialELFBalance >= thresholdELF)
        {
            freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(firstFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(firstFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(firstFreeAmountELF);

            freeAllowances.Map.Values.First().Map.Keys.Last().ShouldBe(secondFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.Last().Symbol.ShouldBe(secondFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.Last().Amount.ShouldBe(secondFreeAmountELF);
        }

        if (initialUSDTBalance >= thresholdUSDT)
        {
            freeAllowances.Map.Keys.Last().ShouldBe(USDT);
            freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(firstFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(firstFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(firstFreeAmountUSDT);
            
            freeAllowances.Map.Values.Last().Map.Keys.Last().ShouldBe(secondFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.Last().Symbol.ShouldBe(secondFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.Last().Amount.ShouldBe(secondFreeAmountUSDT);
        }

        var chargeTransactionFeesInput = new ChargeTransactionFeesInput
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
            ContractAddress = TokenContractAddress,
            TransactionSizeFee = sizeFee,
        };
        chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);
        
        var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
        chargeFeeRet.Output.Success.ShouldBe(chargeFeeResult);

        freeAllowances = await TokenContractImplStub.GetMethodFeeFreeAllowances.CallAsync(DefaultSender);
        if (afterELFBalance >= thresholdELF)
        {
            freeAllowances.Map.Keys.First().ShouldBe(NativeTokenSymbol);
            freeAllowances.Map.Values.First().Map.Keys.First().ShouldBe(firstFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.First().Symbol.ShouldBe(firstFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.First().Amount.ShouldBe(newFirstFreeAmountELF);

            freeAllowances.Map.Values.First().Map.Keys.Last().ShouldBe(secondFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.Last().Symbol.ShouldBe(secondFreeSymbolELF);
            freeAllowances.Map.Values.First().Map.Values.Last().Amount.ShouldBe(newSecondFreeAmountELF);
        }

        if (afterUSDTBalance >= thresholdUSDT)
        {
            freeAllowances.Map.Keys.Last().ShouldBe(USDT);
            freeAllowances.Map.Values.Last().Map.Keys.First().ShouldBe(firstFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.First().Symbol.ShouldBe(firstFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.First().Amount.ShouldBe(newFirstFreeAmountUSDT);
            
            freeAllowances.Map.Values.Last().Map.Keys.Last().ShouldBe(secondFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.Last().Symbol.ShouldBe(secondFreeSymbolUSDT);
            freeAllowances.Map.Values.Last().Map.Values.Last().Amount.ShouldBe(newSecondFreeAmountUSDT);
        }

        await CheckDefaultSenderTokenAsync(Token1, afterToken1Balance);
        await CheckDefaultSenderTokenAsync(Token2, afterToken2Balance);
        await CheckDefaultSenderTokenAsync(NativeTokenSymbol, afterELFBalance);
        await CheckDefaultSenderTokenAsync(USDT, afterUSDTBalance);
    }

    private async Task CheckDefaultSenderTokenAsync(string symbol, long amount)
    {
        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultSender,
            Symbol = symbol
        });

        balance.Balance.ShouldBe(amount);
    }

    private async Task CreateTokenAndIssueAsync()
    {
        await CreateTokenAsync(DefaultSender, USDT);
        await IssueTokenToUserAsync(NativeTokenSymbol, 1_00000000, UserAAddress);
        await IssueTokenToUserAsync(USDT, 1_000000, UserBAddress);
        await IssueTokenToUserAsync(NativeTokenSymbol, 1_00000000, UserCAddress);
        await IssueTokenToUserAsync(USDT, 1_000000, UserCAddress);
    }
}