using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public static class NftCollectionMetaFields
{
    public static string ImageUrlKey = "__nft_image_url";
    public static string BaseUriKey = "__nft_base_uri";
    public static string NftType = "__nft_type";
    public const string IsItemIdReuseKey = "__nft_is_item_id_reuse";
}

public static class NftInfoMetaFields
{
    public static string ImageUrlKey = "__nft_image_url";
    public static string IsBurnedKey = "__nft_is_burned";
}

public partial class MultiTokenContractTests
{
    private readonly string NftType = "ART";

    private TokenInfo NftCollection721Info => new()
    {
        Symbol = "TT-",
        TokenName = "Trump Digital Trading Cards #721",
        TotalSupply = 1,
        Decimals = 0,
        Issuer = Accounts[0].Address,
        IssueChainId = _chainId,
        ExternalInfo = new ExternalInfo()
        {
            Value =
            {
                {
                    NftCollectionMetaFields.ImageUrlKey,
                    "https://i.seadn.io/gcs/files/0f5cdfaaf687de2ebb5834b129a5bef3.png?auto=format&w=3840"
                }
            }
        }
    };

    private TokenInfo NftCollection1155Info => new()
    {
        Symbol = "TP-",
        TokenName = "Trump Digital Trading Cards #1155",
        TotalSupply = TotalSupply,
        Decimals = 0,
        Issuer = Accounts[0].Address,
        IssueChainId = _chainId,
        ExternalInfo = new ExternalInfo()
        {
            Value =
            {
                {
                    NftCollectionMetaFields.ImageUrlKey,
                    "https://i.seadn.io/gcs/files/0f5cdfaaf687de2ebb5834b129a5bef3.png?auto=format&w=3840"
                },
                { NftCollectionMetaFields.NftType, NftType }
            }
        }
    };

    private TokenInfo Nft721Info => new()
    {
        Symbol = "31175",
        TokenName = "Trump Digital Trading Card #31175",
        TotalSupply = 1,
        Decimals = 0,
        Issuer = Accounts[0].Address,
        IssueChainId = _chainId,
        IsBurnable = true,
        ExternalInfo = new ExternalInfo()
        {
            Value =
            {
                {
                    NftInfoMetaFields.ImageUrlKey,
                    "https://i.seadn.io/gcs/files/0f5cdfaaf687de2ebb5834b129a5bef3.png?auto=format&w=3840"
                }
            }
        }
    };

    private TokenInfo Nft1155Info => new()
    {
        Symbol = "12419",
        TokenName = "Trump Digital Trading Card #12419",
        TotalSupply = TotalSupply,
        Decimals = 0,
        Issuer = Accounts[0].Address,
        IssueChainId = _chainId,
        IsBurnable = false,
        ExternalInfo = new ExternalInfo()
        {
            Value =
            {
                {
                    NftInfoMetaFields.ImageUrlKey,
                    "https://i.seadn.io/gcs/files/0f5cdfaaf687de2ebb5834b129a5bef3.png?auto=format&w=3840"
                },
                { NftInfoMetaFields.IsBurnedKey, "false" }
            }
        }
    };

    private async Task<IExecutionResult<Empty>> CreateNftCollectionAsync(TokenInfo collectionInfo)
    {
        await CreateNativeTokenAsync();
        return await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = $"{collectionInfo.Symbol}0",
            TokenName = collectionInfo.TokenName,
            TotalSupply = collectionInfo.TotalSupply,
            Decimals = collectionInfo.Decimals,
            Issuer = collectionInfo.Issuer,
            IssueChainId = collectionInfo.IssueChainId,
            ExternalInfo = collectionInfo.ExternalInfo
        });
    }

    private async Task<IExecutionResult<Empty>> CreateNftAsync(string colllectionSymbol, TokenInfo nftInfo)
    {
        return await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = $"{colllectionSymbol}{nftInfo.Symbol}",
            TokenName = nftInfo.TokenName,
            TotalSupply = nftInfo.TotalSupply,
            Decimals = nftInfo.Decimals,
            Issuer = nftInfo.Issuer,
            IsBurnable = nftInfo.IsBurnable,
            IssueChainId = nftInfo.IssueChainId,
            ExternalInfo = nftInfo.ExternalInfo
        });
    }

    private async Task<List<string>> CreateNftCollectionAndNft(bool reuseItemId = true)
    {
        var symbols = new List<string>();
        var collectionInfo = reuseItemId ? NftCollection1155Info : NftCollection721Info;
        var createCollectionRes = await CreateNftCollectionAsync(collectionInfo);
        createCollectionRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var createCollectionLog = TokenCreated.Parser.ParseFrom(createCollectionRes.TransactionResult.Logs
            .First(l => l.Name == nameof(TokenCreated)).NonIndexed);
        var collectionSymbolWords = createCollectionLog.Symbol.Split("-");
        Assert.True(collectionSymbolWords.Length == 2);
        AssertTokenEqual(createCollectionLog, collectionInfo);
        symbols.Add(createCollectionLog.Symbol);
        createCollectionLog.Symbol.ShouldBe(collectionInfo.Symbol + "0");

        var createNftRes = await CreateNftAsync(collectionInfo.Symbol, Nft721Info);
        createNftRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var createNftLog = TokenCreated.Parser.ParseFrom(createNftRes.TransactionResult.Logs
            .First(l => l.Name == nameof(TokenCreated)).NonIndexed);
        var nftSymbolWords = createNftLog.Symbol.Split("-");
        Assert.True(nftSymbolWords.Length == 2);
        Assert.Equal(nftSymbolWords[0], collectionSymbolWords[0]);
        AssertTokenEqual(createNftLog, Nft721Info);
        symbols.Add(createNftLog.Symbol);
        createNftLog.Symbol.ShouldBe(collectionInfo.Symbol + Nft721Info.Symbol);

        var createNft2Res = await CreateNftAsync(collectionInfo.Symbol, Nft1155Info);
        createNft2Res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var createNft2Log = TokenCreated.Parser.ParseFrom(createNft2Res.TransactionResult.Logs
            .First(l => l.Name == nameof(TokenCreated)).NonIndexed);
        var nft2SymbolWords = createNft2Log.Symbol.Split("-");
        Assert.True(nft2SymbolWords.Length == 2);
        Assert.Equal(nft2SymbolWords[0], collectionSymbolWords[0]);
        AssertTokenEqual(createNft2Log, Nft1155Info);
        symbols.Add(createNft2Log.Symbol);
        createNft2Log.Symbol.ShouldBe(collectionInfo.Symbol + Nft1155Info.Symbol);
        return symbols;
    }

    private void AssertTokenEqual(TokenCreated log, TokenInfo input)
    {
        Assert.Equal(log.TokenName, input.TokenName);
        Assert.Equal(log.TotalSupply, input.TotalSupply);
        Assert.Equal(log.Decimals, input.Decimals);
        Assert.Equal(log.Issuer, input.Issuer);
        Assert.Equal(log.IssueChainId, input.IssueChainId);
        foreach (var kv in input.ExternalInfo.Value)
        {
            Assert.True(log.ExternalInfo.Value.ContainsKey(kv.Key));
            Assert.Equal(log.ExternalInfo.Value[kv.Key], kv.Value);
        }
    }

    [Fact(DisplayName = "[MultiToken_Nft] Create 1155 nfts.")]
    public async Task MultiTokenContract_Create_1155Nft_Test()
    {
        await CreateNftCollectionAndNft();
    }

    [Fact(DisplayName = "[MultiToken_Nft] Create 721 nfts.")]
    public async Task MultiTokenContract_Create_721Nft_Test()
    {
        await CreateNftCollectionAndNft(false);
    }

    [Fact(DisplayName = "[MultiToken_Nft] Create nft collection input check")]
    public async Task MultiTokenContract_Create_NFTCollection_Input_Check_Test()
    {
        await CreateNativeTokenAsync();
        var input = NftCollection721Info;
        // Decimals check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = $"{input.Symbol}0",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = 8,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("NFT's decimals must be 0");
        }
        // Symbol check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = "ABC123",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Invalid Symbol input");
        }
        // Symbol length check 
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = "ABCDEFGHIJKLMNOPQRSTUVWXYZABC-0",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Invalid NFT symbol length");
        }
        // Issue chain Id check
        {
            var result = await TokenContractStubUser.Create.SendAsync(new CreateInput
            {
                Symbol =  $"{input.Symbol}0",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = NftCollection721Info.Issuer,
                IssueChainId = ChainHelper.ConvertBase58ToChainId("tDVV"),
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Fact(DisplayName = "[MultiToken_Nft] Create nft input check")]
    public async Task MultiTokenContract_Create_NFT_Input_Check_Test()
    {
        await CreateNftCollectionAsync(NftCollection721Info);
        var input = Nft721Info;

        // Decimals check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = $"{NftCollection721Info.Symbol}{input.Symbol}",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = 8,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("NFT's decimals must be 0");
        }
        // Symbol check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = "ABC-ABC",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Invalid NFT Symbol input");
        }
        // Symbol check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = "ABC-",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Invalid NFT Symbol input");
        }
        // Symbol check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = "ABC-ABC-1",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Invalid NFT Symbol input");
        }
        // Issue check
        {
            var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = $"{NftCollection721Info.Symbol}{input.Symbol}",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = Accounts.Last().Address,
                IssueChainId = input.IssueChainId,
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("NFT issuer must be collection's issuer");
        }
        {
            var result = await TokenContractStubUser.Create.SendWithExceptionAsync(new CreateInput
            {
                Symbol = $"{NftCollection721Info.Symbol}{input.Symbol}",
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IssueChainId = ChainHelper.ConvertBase58ToChainId("tDVV"),
                ExternalInfo = input.ExternalInfo
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("NFT create ChainId must be collection's issue chainId");
        }
    }

    [Fact(DisplayName = "[MultiToken_Nft] Collection not exist")]
    public async Task MultiTokenContract_Create_NFT_Collection_NotExist()
    {
        await CreateNativeTokenAsync();
        var input = Nft721Info;
        var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
        {
            Symbol = $"{NftCollection721Info.Symbol}{input.Symbol}",
            TokenName = input.TokenName,
            TotalSupply = input.TotalSupply,
            Decimals = input.Decimals,
            Issuer = input.Issuer,
            IssueChainId = input.IssueChainId,
            ExternalInfo = input.ExternalInfo
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("NFT collection not exist");
    }

    [Fact(DisplayName = "[MultiToken_Nft] Create already exist nft")]
    public async Task MultiTokenContract_Create_NFT_Already_Exist()
    {
        await CreateNftCollectionAndNft(false);
        var input = Nft721Info;
        var result = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
        {
            Symbol = $"{NftCollection721Info.Symbol}{input.Symbol}",
            TokenName = input.TokenName,
            TotalSupply = input.TotalSupply,
            Decimals = input.Decimals,
            Issuer = input.Issuer,
            IssueChainId = input.IssueChainId,
            ExternalInfo = input.ExternalInfo
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("Token already exists.");
    }

    [Fact(DisplayName = "[MultiToken_Nft] Issue and transfer 1155 nfts.")]
    public async Task NftIssueAndTransferTest()
    {
        var symbols = await CreateNftCollectionAndNft();
        Assert.True(symbols.Count == 3);
        var symbol = symbols[2];
        var issueRes = await TokenContractStub.Issue.SendAsync(new IssueInput()
        {
            Symbol = symbol,
            Amount = 100,
            To = DefaultAddress,
            Memo = "Issue Nft"
        });
        issueRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var nftInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput() { Symbol = symbol });
        nftInfo.Supply.ShouldBe(100);
        nftInfo.Issuer.ShouldBe(NftCollection721Info.Issuer);

        var transferRes = await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 10,
            Memo = "transfer nft test",
            Symbol = symbol,
            To = User1Address
        });
        transferRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var defaultBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = symbol,
            Owner = DefaultAddress
        });
        var user1BalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = symbol,
            Owner = User1Address
        });

        defaultBalanceOutput.Balance.ShouldBe(90);
        user1BalanceOutput.Balance.ShouldBe(10);
    }

    [Fact(DisplayName = "[MultiToken_Nft] Issue and transfer 721 nfts.")]
    public async Task Nft721IssueAndTransferTest()
    {
        var symbols = await CreateNftCollectionAndNft(false);
        Assert.True(symbols.Count == 3);
        var symbol = symbols[1];
        var issueRes = await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = symbol,
            Amount = 1,
            To = DefaultAddress,
            Memo = "Issue Nft"
        });
        issueRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var nftInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput() { Symbol = symbol });
        nftInfo.Supply.ShouldBe(1);
        nftInfo.Issuer.ShouldBe(NftCollection721Info.Issuer);

        var transferRes = await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = 10,
            Memo = "transfer nft test",
            Symbol = symbol,
            To = User1Address
        });
        transferRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

        transferRes = await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 1,
            Memo = "transfer nft test",
            Symbol = symbol,
            To = User1Address
        });
        transferRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var defaultBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = symbol,
            Owner = DefaultAddress
        });
        var user1BalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = symbol,
            Owner = User1Address
        });

        defaultBalanceOutput.Balance.ShouldBe(0);
        user1BalanceOutput.Balance.ShouldBe(1);
    }

    [Fact(DisplayName = "[MultiToken_Nft] Issue and transfer out of amount")]
    public async Task NftIssueAndTransfer_OutOfAmount_Test()
    {
        var symbols = await CreateNftCollectionAndNft();
        Assert.True(symbols.Count == 3);

        var transferRes = await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput()
        {
            Amount = 10,
            Memo = "transfer nft test",
            Symbol = symbols[1],
            To = DefaultAddress
        });
        transferRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transferRes.TransactionResult.Error.ShouldContain("Can't do transfer to sender itself");

        transferRes = await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput()
        {
            Amount = 10,
            Memo = "transfer nft test",
            Symbol = symbols[1],
            To = User1Address
        });
        transferRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transferRes.TransactionResult.Error.ShouldContain("Insufficient balance");
    }

    [Fact(DisplayName = "[MultiToken_Nft] Issue and burn not exist nft")]
    public async Task NftIssue_Burn_NotExist_Token()
    {
        var notExistToken = "ABC-1";
        var issueRes = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
        {
            Symbol = notExistToken,
            Amount = 1,
            To = DefaultAddress,
            Memo = "Issue Nft"
        });
        issueRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        issueRes.TransactionResult.Error.ShouldContain("Token is not found.");

        var burnRes = await TokenContractStub.Burn.SendWithExceptionAsync(new BurnInput
        {
            Symbol = notExistToken,
            Amount = 1
        });
        burnRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        burnRes.TransactionResult.Error.ShouldContain("Token is not found.");
    }

    [Fact(DisplayName = "[MultiToken_Nft] 721 nfts Burn Test")]
    public async Task NftIssueAndTransferBurn()
    {
        var symbols = await CreateNftCollectionAndNft(false);
        Assert.True(symbols.Count == 3);
        var symbol = symbols[1];
        var issueRes = await TokenContractStub.Issue.SendAsync(new IssueInput()
        {
            Symbol = symbol,
            Amount = 1,
            To = DefaultAddress,
            Memo = "Issue Nft"
        });
        issueRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var res = await TokenContractStub.Burn.SendAsync(new BurnInput
        {
            Amount = 1,
            Symbol = symbol
        });
        res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = symbols[1] });
        tokenInfo.Issued.ShouldBe(1);
        tokenInfo.TotalSupply.ShouldBe(1);
        tokenInfo.Supply.ShouldBe(0);

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = symbol
        });
        balance.Balance.ShouldBe(0);

        var result = (await TokenContractStub.Burn.SendWithExceptionAsync(new BurnInput
        {
            Symbol = symbol,
            Amount = 1
        })).TransactionResult;
        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.ShouldContain("Insufficient balance");

        issueRes = await TokenContractStub.Issue.SendWithExceptionAsync(new IssueInput()
        {
            Symbol = symbol,
            Amount = 1,
            To = DefaultAddress,
            Memo = "Issue Nft"
        });
        issueRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        issueRes.TransactionResult.Error.ShouldContain("Total supply exceeded");
    }

    [Fact(DisplayName = "[MultiToken-nft] 1155 nfts approve and transferFrom Test")]
    public async Task NftIssue_Approve_TransferFrom()
    {
        var symbols = await CreateNftCollectionAndNft();
        Assert.True(symbols.Count == 3);
        var symbol = symbols[2];
        var spender = Accounts.Last().Address;
        var issueRes = await TokenContractStub.Issue.SendAsync(new IssueInput()
        {
            Symbol = symbol,
            Amount = 100,
            To = DefaultAddress,
            Memo = "Issue Nft"
        });
        issueRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var approveSpenderResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Symbol = symbol,
            Amount = 10,
            Spender = spender
        })).TransactionResult;
        approveSpenderResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var spenderAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = spender,
            Symbol = symbol
        });
        spenderAllowanceOutput.Allowance.ShouldBe(10);

        var spenderStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, Accounts.Last().KeyPair);
        var transferFrom = await spenderStub.TransferFrom.SendAsync(new TransferFromInput
        {
            Symbol = symbol,
            Amount = 10,
            From = DefaultAddress,
            To = spender
        });
        transferFrom.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        spenderAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = spender,
            Symbol = symbol
        });
        spenderAllowanceOutput.Allowance.ShouldBe(0);

        var spenderBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = spender,
            Symbol = symbol
        });
        spenderBalanceOutput.Balance.ShouldBe(10);

        var ownerBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = symbol
        });
        ownerBalanceOutput.Balance.ShouldBe(90);
    }
}