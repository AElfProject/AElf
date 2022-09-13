using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMinter;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT;

public partial class NFTContractTests
{
    [Fact]
    public async Task<string> BoxTest()
    {
        await InitializeNFTMinterContractAsync();

        var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
        {
            ProtocolName = "Virtual World Something",
            NftType = NFTType.VirtualWorlds.ToString(),
            TotalSupply = 100_000,
            IsBurnable = false,
            IsTokenIdReuse = false,
            MinterList = new MinterList
            {
                Value = { NFTMinterContractAddress }
            }
        });
        var symbol = executionResult.Output.Value;

        await CreatorNFTMinterContractStub.Box.SendAsync(new BoxInput
        {
            Symbol = symbol,
            IsTokenIdFixed = false,
            TemplateList = new NFTTemplateList
            {
                Value =
                {
                    new NFTTemplate
                    {
                        Symbol = symbol,
                        Alias = "Normal",
                        Metadata = new TemplateMetadata
                        {
                            Value = { ["Quality"] = "Normal", ["Value"] = "10" }
                        },
                        Quantity = 700,
                        TokenId = 1,
                        Weight = 100
                    },
                    new NFTTemplate
                    {
                        Symbol = symbol,
                        Alias = "Rare",
                        Metadata = new TemplateMetadata
                        {
                            Value = { ["Quality"] = "Rare", ["Value"] = "200" }
                        },
                        Quantity = 200,
                        TokenId = 701,
                        Weight = 10
                    },
                    new NFTTemplate
                    {
                        Symbol = symbol,
                        Alias = "Legend",
                        Metadata = new TemplateMetadata
                        {
                            Value = { ["Quality"] = "Legend", ["Value"] = "1000" }
                        },
                        Quantity = 100,
                        TokenId = 901,
                        Weight = 1
                    }
                }
            },
            CostSymbol = "ELF",
            CostAmount = 1_00000000
        });

        var blindBoxInfo = await CreatorNFTMinterContractStub.GetBlindBoxInfo.CallAsync(new GetBlindBoxInfoInput
        {
            Symbol = symbol
        });
        blindBoxInfo.SupposedEndTokenId.ShouldBe(1000);
        return symbol;
    }

    [Fact]
    public async Task UnboxTest()
    {
        var symbol = await BoxTest();

        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            To = User1Address,
            Amount = 10000_00000000
        });

        await UserTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
        {
            Symbol = "ELF",
            Amount = 10000_00000000,
            Spender = NFTMinterContractAddress
        });

        await UserNFTMinterContractStub.Unbox.SendAsync(new UnboxInput
        {
            Symbol = symbol
        });

        {
            var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = 1,
                Owner = User1Address
            });
            balance.Balance.ShouldBe(1);
        }
    }
}