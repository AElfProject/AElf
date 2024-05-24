using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    public const string TokenAliasExternalInfoKey = "aelf_token_alias";

    [Fact]
    public async Task SetTokenAlias_NFTCollection_Test()
    {
        var symbols = await CreateNftCollectionAndNft();
        await TokenContractStub.SetSymbolAlias.SendAsync(new SetSymbolAliasInput
        {
            Symbol = symbols[1],
            Alias = "TP"
        });

        {
            // Check TokenInfo of NFT Collection.
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbols[0]
            });
            tokenInfo.ExternalInfo.Value.ContainsKey(TokenAliasExternalInfoKey);
            tokenInfo.ExternalInfo.Value[TokenAliasExternalInfoKey].ShouldBe("{\"TP-31175\":\"TP\"}");
        }

        {
            // Check TokenInfo of NFT Item.
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "TP"
            });
            tokenInfo.Symbol.ShouldBe(symbols[1]);
        }

        {
            // Check alias.
            var alias = await TokenContractStub.GetTokenAlias.CallAsync(new StringValue { Value = "TP-31175" });
            alias.Value.ShouldBe("TP");
        }

        {
            var alias = await TokenContractStub.GetSymbolByAlias.CallAsync(new StringValue { Value = "TP" });
            alias.Value.ShouldBe("TP-31175");
        }
    }

    [Fact]
    public async Task SetTokenAlias_NFTCollection_CollectionSymbol_Test()
    {
        await CreateNftCollectionAndNft();
        await TokenContractStub.SetSymbolAlias.SendAsync(new SetSymbolAliasInput
        {
            Symbol = "TP-0",
            Alias = "TP"
        });

        {
            // Check TokenInfo of NFT Collection.
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "TP-0"
            });
            tokenInfo.ExternalInfo.Value.ContainsKey(TokenAliasExternalInfoKey);
            tokenInfo.ExternalInfo.Value[TokenAliasExternalInfoKey].ShouldBe("{\"TP-0\":\"TP\"}");
        }

        {
            // Check TokenInfo of NFT Item.
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "TP"
            });
            tokenInfo.Symbol.ShouldBe("TP-0");
        }

        {
            // Check alias.
            var alias = await TokenContractStub.GetTokenAlias.CallAsync(new StringValue { Value = "TP-0" });
            alias.Value.ShouldBe("TP");
        }

        {
            var alias = await TokenContractStub.GetSymbolByAlias.CallAsync(new StringValue { Value = "TP" });
            alias.Value.ShouldBe("TP-0");
        }
    }

    [Fact]
    public async Task SetTokenAlias_FT_Test()
    {
        await CreateNormalTokenAsync();

        // Set token alias for FT.
        var result = await TokenContractStub.SetSymbolAlias.SendWithExceptionAsync(new SetSymbolAliasInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
        });
        result.TransactionResult.Error.ShouldContain("Token alias can only be set for NFT Item.");
    }

    [Fact]
    public async Task CreateTokenWithAlias_Test()
    {
        var createCollectionResult = await CreateNftCollectionAsync(NftCollection1155WithAliasInfo);
        createCollectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        await CreateNftAsync(NftCollection1155WithAliasInfo.Symbol, Nft721Info);

        {
            // Check alias.
            var alias = await TokenContractStub.GetTokenAlias.CallAsync(new StringValue { Value = "TP-31175" });
            alias.Value.ShouldBe("TP");
        }

        {
            // Check TokenInfo of NFT Item.
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "TP"
            });
            tokenInfo.Symbol.ShouldBe("TP-31175");
        }
    }

    [Fact]
    public async Task CreateTokenWithAlias_FT_Test()
    {
        var createInput = new CreateInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            TokenName = AliceCoinTokenInfo.TokenName,
            TotalSupply = AliceCoinTokenInfo.TotalSupply,
            Decimals = AliceCoinTokenInfo.Decimals,
            Issuer = AliceCoinTokenInfo.Issuer,
            Owner = AliceCoinTokenInfo.Issuer,
            IsBurnable = AliceCoinTokenInfo.IsBurnable,
            LockWhiteList =
            {
                BasicFunctionContractAddress,
                OtherBasicFunctionContractAddress,
                TokenConverterContractAddress,
                TreasuryContractAddress
            },
            ExternalInfo = new ExternalInfo
            {
                Value =
                {
                    { TokenAliasExternalInfoKey, "{\"ALICE-111\":\"ALICE\"}" }
                }
            }
        };
        await CreateSeedNftAsync(TokenContractStub, createInput);
        var result = await TokenContractStub.Create.SendWithExceptionAsync(createInput);
        result.TransactionResult.Error.ShouldContain("Token alias can only be set for NFT Item.");
    }

    [Fact]
    public async Task TransferViaAlias_Test()
    {
        await CreateTokenWithAlias_Test();

        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "TP-31175",
            Amount = 1,
            To = DefaultAddress
        });

        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = "TP"
            });
            balance.Balance.ShouldBe(1);
        }

        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            // Transfer via alias.
            Symbol = "TP",
            Amount = 1,
            To = User1Address
        });

        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = "TP"
            });
            balance.Balance.ShouldBe(1);
        }
    }

    [Fact]
    public async Task ApproveAndTransferFromViaAlias_Test()
    {
        await CreateTokenWithAlias_Test();

        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "TP-31175",
            Amount = 1,
            To = DefaultAddress
        });

        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Symbol = "TP",
            Amount = 1,
            Spender = User1Address
        });

        await TokenContractStubUser.TransferFrom.SendAsync(new TransferFromInput
        {
            Symbol = "TP",
            Amount = 1,
            From = DefaultAddress,
            To = User2Address,
        });

        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User2Address,
                Symbol = "TP"
            });
            balance.Balance.ShouldBe(1);
        }
    }

    [Fact]
    public async Task GetBalanceOfNotExistToken_Test()
    {
        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = User2Address,
            Symbol = "TP"
        });
        balance.Balance.ShouldBe(0);
    }

    [Fact]
    public async Task GetAllowanceOfNotExistToken_Test()
    {
        var allowance = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = User2Address,
            Symbol = "TP",
            Spender = DefaultAddress
        });
        allowance.Allowance.ShouldBe(0);
    }

    [Fact]
    public async Task BatchApproveWithAlias_Test()
    {
        await SetTokenAlias_NFTCollection_Test();
        await CreateTokenAndIssue();
        var approveBasisResult = (await TokenContractStub.BatchApprove.SendAsync(new BatchApproveInput
        {
            Value =
            {
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 2000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = "TP",
                    Amount = 1000L,
                    Spender = OtherBasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 5000L,
                    Spender = TreasuryContractAddress
                }
            }
        })).TransactionResult;
        approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var basicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = BasicFunctionContractAddress,
            Symbol = SymbolForTest
        });
        basicAllowanceOutput.Allowance.ShouldBe(2000L);
        var otherBasicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = OtherBasicFunctionContractAddress,
            Symbol = "TP"
        });
        otherBasicAllowanceOutput.Allowance.ShouldBe(1000L);
        var treasuryAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = TreasuryContractAddress,
            Symbol = SymbolForTest
        });
        treasuryAllowanceOutput.Allowance.ShouldBe(5000L);

        approveBasisResult = (await TokenContractStub.BatchApprove.SendAsync(new BatchApproveInput
        {
            Value =
            {
                new ApproveInput
                {
                    Symbol = "TP",
                    Amount = 1000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 3000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 3000L,
                    Spender = TreasuryContractAddress
                }
            }
        })).TransactionResult;
        approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);
        basicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = BasicFunctionContractAddress,
            Symbol = SymbolForTest
        });
        basicAllowanceOutput.Allowance.ShouldBe(3000L);

        treasuryAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = TreasuryContractAddress,
            Symbol = SymbolForTest
        });
        treasuryAllowanceOutput.Allowance.ShouldBe(3000L);
    }

    private TokenInfo NftCollection1155WithAliasInfo => new()
    {
        Symbol = "TP-",
        TokenName = "Trump Digital Trading Cards #1155",
        TotalSupply = TotalSupply,
        Decimals = 0,
        Issuer = DefaultAddress,
        IssueChainId = _chainId,
        ExternalInfo = new ExternalInfo
        {
            Value =
            {
                {
                    NftCollectionMetaFields.ImageUrlKey,
                    "https://i.seadn.io/gcs/files/0f5cdfaaf687de2ebb5834b129a5bef3.png?auto=format&w=3840"
                },
                { NftCollectionMetaFields.NftType, NftType },
                { TokenAliasExternalInfoKey, "{\"TP-31175\":\"TP\"}" }
            }
        }
    };
}