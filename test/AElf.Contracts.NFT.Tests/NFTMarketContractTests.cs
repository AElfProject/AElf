using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractTests
    {
        private const long InitialELFAmount = 1_00000000_00000000;

        [Fact]
        public async Task<string> ListWithFixedPriceTest()
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });

            var symbol = await MintBadgeTest();

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

            await NFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = symbol,
                TokenId = 233,
                Amount = 1,
                Spender = NFTMarketContractAddress
            });

            await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 100_00000000
                },
                Duration = new Duration
                {
                    DurationMinutes = 60 * 24
                },
                Description = "A Badge.",
                Quantity = 1
            });

            var listedNftInfo = await SellerNFTMarketContractStub.GetListedNFTInfo.CallAsync(new GetListedNFTInfoInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = DefaultAddress
            });
            listedNftInfo.Price.Symbol.ShouldBe("ELF");
            listedNftInfo.Price.Amount.ShouldBe(100_00000000);
            listedNftInfo.Quantity.ShouldBe(1);
            listedNftInfo.ListType.ShouldBe(ListType.FixedPrice);
            listedNftInfo.Duration.StartTime.ShouldNotBeNull();
            listedNftInfo.Duration.DurationMinutes.ShouldBe(60 * 24);

            return symbol;
        }

        [Fact]
        public async Task DealWithFixedPriceTest()
        {
            var symbol = await ListWithFixedPriceTest();

            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = DefaultAddress,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 1000_00000000
                },
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 100_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // Because of 10/10000 service fee.
                balance.Balance.ShouldBe(InitialELFAmount + 99_90000000);
            }

            {
                var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = User2Address
                });
                nftBalance.Balance.ShouldBe(1);
            }

            {
                var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = DefaultAddress
                });
                nftBalance.Balance.ShouldBe(0);
            }
        }
    }
}