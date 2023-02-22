using System.Threading.Tasks;
using AElf.Contracts.TokenConverter;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

// ReSharper disable CheckNamespace
namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests : MultiTokenContractTestBase
{
    private const long TotalSupply = 1000_000_000_00000000;
    private readonly int _chainId;

    private readonly Connector BaseConnector = new()
    {
        Symbol = "ELF",
        VirtualBalance = 0,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = false
    };

    private readonly Connector RamConnector = new()
    {
        Symbol = "ALICE",
        VirtualBalance = 0,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = false,
        RelatedSymbol = "ELF"
    };

    public MultiTokenContractTests()
    {
        _chainId = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value.ChainId;
    }

    private TokenInfo NativeTokenInfo => new()
    {
        Symbol = GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>().Value
            .ContextVariables[ContextVariableDictionary.NativeSymbolName],
        TokenName = "Native token",
        TotalSupply = TotalSupply,
        Decimals = 8,
        IsBurnable = true,
        Issuer = Accounts[0].Address,
        Supply = 0,
        IssueChainId = _chainId
    };

    private TokenInfo PrimaryTokenInfo => new()
    {
        Symbol = "PRIMARY",
        TokenName = "Primary token",
        TotalSupply = TotalSupply,
        Decimals = 8,
        IsBurnable = true,
        Issuer = Accounts[0].Address,
        Supply = 0,
        IssueChainId = _chainId
    };

    /// <summary>
    ///     Burnable & Transferable
    /// </summary>
    private TokenInfo AliceCoinTokenInfo => new()
    {
        Symbol = "ALICE",
        TokenName = "For testing multi-token contract",
        TotalSupply = TotalSupply,
        Decimals = 8,
        IsBurnable = true,
        Issuer = Accounts[0].Address,
        Supply = 0,
        IssueChainId = _chainId,
        ExternalInfo = new ExternalInfo()
    };

    /// <summary>
    ///     Not Burnable & Transferable
    /// </summary>
    private TokenInfo BobCoinTokenInfo => new()
    {
        Symbol = "BOB",
        TokenName = "For testing multi-token contract",
        TotalSupply = 1_000_000_000_0000,
        Decimals = 4,
        IsBurnable = false,
        Issuer = Accounts[0].Address,
        Supply = 0
    };

    /// <summary>
    ///     Not Burnable & Not Transferable
    /// </summary>
    private TokenInfo EanCoinTokenInfo => new()
    {
        Symbol = "EAN",
        TokenName = "For testing multi-token contract",
        TotalSupply = 1_000_000_000,
        Decimals = 0,
        IsBurnable = true,
        Issuer = Accounts[0].Address,
        Supply = 0
    };

    private async Task CreateNativeTokenAsync()
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = NativeTokenInfo.Symbol,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = NativeTokenInfo.TotalSupply,
            Decimals = NativeTokenInfo.Decimals,
            Issuer = NativeTokenInfo.Issuer,
            IsBurnable = NativeTokenInfo.IsBurnable,
            LockWhiteList =
            {
                OtherBasicFunctionContractAddress,
                TokenConverterContractAddress,
                TreasuryContractAddress
            }
        });
    }

    private async Task CreatePrimaryTokenAsync()
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = NativeTokenInfo.Symbol,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = NativeTokenInfo.TotalSupply,
            Decimals = NativeTokenInfo.Decimals,
            Issuer = NativeTokenInfo.Issuer,
            IsBurnable = NativeTokenInfo.IsBurnable
        });

        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Decimals = PrimaryTokenInfo.Decimals,
            IsBurnable = PrimaryTokenInfo.IsBurnable,
            Issuer = PrimaryTokenInfo.Issuer,
            TotalSupply = PrimaryTokenInfo.TotalSupply,
            Symbol = PrimaryTokenInfo.Symbol,
            TokenName = PrimaryTokenInfo.TokenName,
            IssueChainId = PrimaryTokenInfo.IssueChainId
        });

        await TokenContractStub.SetPrimaryTokenSymbol.SendAsync(
            new SetPrimaryTokenSymbolInput
            {
                Symbol = PrimaryTokenInfo.Symbol
            });
    }

    private async Task CreateNormalTokenAsync()
    {
        // Check token information before creating.
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = AliceCoinTokenInfo.Symbol
            });
            tokenInfo.ShouldBe(new TokenInfo());
        }

        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            TokenName = AliceCoinTokenInfo.TokenName,
            TotalSupply = AliceCoinTokenInfo.TotalSupply,
            Decimals = AliceCoinTokenInfo.Decimals,
            Issuer = AliceCoinTokenInfo.Issuer,
            IsBurnable = AliceCoinTokenInfo.IsBurnable,
            LockWhiteList =
            {
                BasicFunctionContractAddress,
                OtherBasicFunctionContractAddress,
                TokenConverterContractAddress,
                TreasuryContractAddress
            }
        });

        // Check token information after creating.
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = AliceCoinTokenInfo.Symbol
            });
            tokenInfo.ShouldBe(AliceCoinTokenInfo);
        }
    }

    private async Task TokenConverterConverterAsync()
    {
        await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());

        await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());

        await TokenConverterContractStub.Initialize.SendAsync(new InitializeInput
        {
            Connectors = { RamConnector, BaseConnector },
            BaseTokenSymbol = "ELF",
            FeeRate = "0.2"
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = 1000L,
            Memo = "ddd",
            To = TokenConverterContractAddress
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = 1000L,
            Memo = "ddd",
            To = TokenConverterContractAddress
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            Amount = 1000L,
            Memo = "ddd",
            To = TokenConverterContractAddress
        });
    }

    [Fact(DisplayName = "[MultiToken] Create different tokens.")]
    public async Task MultiTokenContract_Create_NotSame_Test()
    {
        await CreateAndIssueMultiTokensAsync();

        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = BobCoinTokenInfo.Symbol,
            TokenName = BobCoinTokenInfo.TokenName,
            TotalSupply = BobCoinTokenInfo.TotalSupply,
            Decimals = BobCoinTokenInfo.Decimals,
            Issuer = BobCoinTokenInfo.Issuer,
            IsBurnable = BobCoinTokenInfo.IsBurnable
        });

        // Check token information after creating.
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = BobCoinTokenInfo.Symbol
            });
            tokenInfo.ShouldNotBe(AliceCoinTokenInfo);
        }
    }

    [Fact(DisplayName = "[MultiToken] Create Token use custom address")]
    public async Task MultiTokenContract_Create_UseCustomAddress_Test()
    {
        var transactionResult = (await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
        {
            Symbol = NativeTokenInfo.Symbol,
            Decimals = 2,
            IsBurnable = true,
            Issuer = DefaultAddress,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = AliceCoinTotalAmount,
            LockWhiteList =
            {
                User1Address
            }
        })).TransactionResult;
        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("Addresses in lock white list should be system contract addresses");
    }

    private async Task CreateAndIssueMultiTokensAsync()
    {
        await CreateNativeTokenAsync();
        await CreateNormalTokenAsync();
        //issue AliceToken amount of 1000_00L to DefaultAddress 
        {
            var result = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTotalAmount,
                To = DefaultAddress,
                Memo = "first issue token."
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            })).Balance;
            balance.ShouldBe(AliceCoinTotalAmount);
        }

        //issue ELF amount of 1000_00L to DefaultAddress 
        {
            var result = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = AliceCoinTotalAmount,
                To = DefaultAddress,
                Memo = "first issue token."
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe(AliceCoinTotalAmount);
        }

        //issue AliceToken amount of 1000L to User1Address 
        {
            var result = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 1000,
                To = User1Address,
                Memo = "first issue token."
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            })).Balance;
            balance.ShouldBe(1000);
        }
        //Issue AliceToken amount of 1000L to User2Address  
        {
            var result = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 1000,
                To = User2Address,
                Memo = "second issue token."
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User2Address,
                Symbol = AliceCoinTokenInfo.Symbol
            })).Balance;
            balance.ShouldBe(1000);
        }
    }

    [Fact(DisplayName = "[MultiToken] Issue out of total amount")]
    public async Task MultiTokenContract_Issue_OutOfAmount_Test()
    {
        await CreateAndIssueMultiTokensAsync();
        //issue AliceToken amount of 1000L to User1Address 
        var result = (await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            Amount = AliceCoinTokenInfo.TotalSupply + 1,
            To = User1Address,
            Memo = "first issue token."
        })).TransactionResult;
        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.Contains("Total supply exceeded").ShouldBeTrue();
    }

    [Fact]
    public async Task IssueToken_With_Invalid_Input()
    {
        await CreateNativeTokenAsync();
        await CreateNormalTokenAsync();
        // to is null
        {
            var result = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTotalAmount
            });
            result.TransactionResult.Error.ShouldContain("To address not filled.");
        }
        // invalid memo
        {
            var result = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTotalAmount,
                To = DefaultAddress,
                Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest.."
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var result = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTotalAmount,
                To = DefaultAddress,
                Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest..."
            });
            result.TransactionResult.Error.ShouldContain("Invalid memo size.");
        }
        // issue token that is not existed
        {
            var result = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = "NOTEXISTED",
                Amount = AliceCoinTotalAmount,
                To = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Token is not found");
        }
        //invalid chain id
        {
            var chainTokenSymbol = "CHAIN";
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = chainTokenSymbol,
                TokenName = "chain token",
                Issuer = DefaultAddress,
                IssueChainId = 10,
                TotalSupply = 1000_000,
                Decimals = 8
            });
            var result = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = chainTokenSymbol,
                Amount = 100,
                To = DefaultAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Unable to issue token with wrong chainId");
        }
    }

    [Fact]
    public async Task IssueToken_Test()
    {
        await CreateNativeTokenAsync();
        var newTokenSymbol = "AIN";
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = newTokenSymbol,
            TokenName = "ain token",
            Issuer = DefaultAddress,
            TotalSupply = 1000_000,
            Decimals = 8
        });
        // invalid input
        {
            var issueRet = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Amount = 10000,
                To = DefaultAddress
            });
            issueRet.TransactionResult.Error.ShouldContain("invalid symbol");
            issueRet = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = "NOTEXIST",
                Amount = 10000,
                To = DefaultAddress
            });
            issueRet.TransactionResult.Error.ShouldContain("Token is not found");
        }
        //invalid amount
        {
            var issueRet = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = newTokenSymbol,
                Amount = -1,
                To = DefaultAddress
            });
            issueRet.TransactionResult.Error.ShouldContain("Invalid amount");
            issueRet = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = newTokenSymbol,
                Amount = 0,
                To = DefaultAddress
            });
            issueRet.TransactionResult.Error.ShouldContain("Invalid amount");
            issueRet = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
            {
                Symbol = newTokenSymbol,
                Amount = 1_000_000_000,
                To = DefaultAddress
            });
            issueRet.TransactionResult.Error.ShouldContain("Total supply exceeded");
        }
    }

    [Fact]
    public async Task TokenCreate_Test()
    {
        var createTokenInfo = new CreateInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            TokenName = AliceCoinTokenInfo.TokenName,
            TotalSupply = AliceCoinTokenInfo.TotalSupply,
            Decimals = AliceCoinTokenInfo.Decimals,
            Issuer = AliceCoinTokenInfo.Issuer,
            IsBurnable = AliceCoinTokenInfo.IsBurnable,
            LockWhiteList =
            {
                BasicFunctionContractAddress,
                OtherBasicFunctionContractAddress,
                TokenConverterContractAddress,
                TreasuryContractAddress
            }
        };
        var createTokenRet = await TokenContractStub.Create.SendWithExceptionAsync(createTokenInfo);
        createTokenRet.TransactionResult.Error.ShouldContain("Invalid native token input");
        // var createTokenInfoWithInvalidTokenName = new CreateInput();
        // createTokenInfoWithInvalidTokenName.MergeFrom(createTokenInfo);
        // createTokenInfoWithInvalidTokenName.Symbol = "ITISAVERYLONGSYMBOLNAME"; 
        // createTokenRet = await TokenContractStub.Create.SendWithExceptionAsync(createTokenInfoWithInvalidTokenName);
        // createTokenRet.TransactionResult.Error.ShouldContain("Invalid input");
        var createTokenInfoWithInvalidDecimal = new CreateInput();
        createTokenInfoWithInvalidDecimal.MergeFrom(createTokenInfo);
        createTokenInfoWithInvalidDecimal.Decimals = 100;
        createTokenRet = await TokenContractStub.Create.SendWithExceptionAsync(createTokenInfoWithInvalidDecimal);
        createTokenRet.TransactionResult.Error.ShouldContain("Invalid input");
        await CreateNativeTokenAsync();
        await TokenContractStub.Create.SendAsync(createTokenInfo);
        var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = AliceCoinTokenInfo.Symbol
        });
        tokenInfo.Issuer.ShouldBe(createTokenInfo.Issuer);
    }

    [Fact]
    public async Task SetPrimaryToken_Test()
    {
        var setPrimaryTokenRet = await TokenContractStub.SetPrimaryTokenSymbol.SendWithExceptionAsync(
            new SetPrimaryTokenSymbolInput
            {
                Symbol = "NOTEXISTED"
            });
        setPrimaryTokenRet.TransactionResult.Error.ShouldContain("Invalid input");
        await CreateNativeTokenAsync();
        await TokenContractStub.SetPrimaryTokenSymbol.SendAsync(new SetPrimaryTokenSymbolInput
        {
            Symbol = NativeTokenInfo.Symbol
        });
        var primaryToken = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());
        primaryToken.Value.ShouldBe(NativeTokenInfo.Symbol);
        setPrimaryTokenRet = await TokenContractStub.SetPrimaryTokenSymbol.SendWithExceptionAsync(
            new SetPrimaryTokenSymbolInput
            {
                Symbol = NativeTokenInfo.Symbol
            });
        setPrimaryTokenRet.TransactionResult.Error.ShouldContain("Failed to set primary token symbol");
    }

    [Fact]
    public async Task GetNativeToken_Test()
    {
        await CreateNativeTokenAsync();
        var tokenInfo = await TokenContractStub.GetNativeTokenInfo.CallAsync(new Empty());
        tokenInfo.Symbol.ShouldBe(NativeTokenInfo.Symbol);
    }
}