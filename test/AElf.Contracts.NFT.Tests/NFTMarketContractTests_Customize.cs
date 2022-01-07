using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractTests
    {
        [Fact]
        public async Task<string> SetCustomizeInfoTest()
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });

            var symbol = await CreateBadgeTest();
            
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialELFAmount,
                To = DefaultAddress,
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialELFAmount,
                To = User2Address,
            });
            await TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });

            await CreatorNFTMarketContractStub.SetCustomizeInfo.SendAsync(new CustomizeInfo
            {
                Symbol = symbol,
                DepositRate = 2000,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 100_00000000
                },
                StakingAmount = 10_00000000,
                WhiteListHours = 10,
                WorkHours = 7
            });

            var customizeInfo =
                await CreatorNFTMarketContractStub.GetCustomizeInfo.CallAsync(new StringValue {Value = symbol});
            customizeInfo.Symbol.ShouldBe(symbol);
            return symbol;
        }

        [Fact]
        public async Task<string> RequestNewNFTTest()
        {
            var symbol = await SetCustomizeInfoTest();
            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 123,
                OfferTo = DefaultAddress,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                ExpireTime = TimestampHelper.GetUtcNow().AddMinutes(30),
                DueTime = TimestampHelper.GetUtcNow().AddMinutes(45),
            });

            var requestInfo = await BuyerNFTMarketContractStub.GetRequestInfo.CallAsync(new GetRequestInfoInput
            {
                Symbol = symbol,
                TokenId = 123
            });
            requestInfo.DepositRate.ShouldBe(2000);
            requestInfo.IsConfirmed.ShouldBeFalse();
            return symbol;
        }
    }
}