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
            var alias = await TokenContractStub.GetTokenAlias.CallAsync(new StringValue { Value = "TP" });
            alias.Value.ShouldBe("TP-31175");
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
            var alias = await TokenContractStub.GetTokenAlias.CallAsync(new StringValue { Value = "TP" });
            alias.Value.ShouldBe("TP-31175");
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
            To = Accounts[1].Address
        });

        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Accounts[1].Address,
                Symbol = "TP"
            });
            balance.Balance.ShouldBe(1);
        }
    }

    private TokenInfo NftCollection1155WithAliasInfo => new()
    {
        Symbol = "TP-",
        TokenName = "Trump Digital Trading Cards #1155",
        TotalSupply = TotalSupply,
        Decimals = 0,
        Issuer = Accounts[0].Address,
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