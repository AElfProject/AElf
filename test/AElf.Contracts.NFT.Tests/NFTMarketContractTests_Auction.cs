using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractTests
    {
        [Fact]
        public async Task<string> ListWithEnglishAuctionTest()
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

            await SellerNFTMarketContractStub.ListWithEnglishAuction.SendAsync(new ListWithEnglishAuctionInput
            {
                Symbol = symbol,
                TokenId = 233,
                Duration = new ListDuration
                {
                    StartTime = TimestampHelper.GetUtcNow(),
                    DurationHours = 100
                },
                PurchaseSymbol = "ELF",
                StartingPrice = 100_00000000
            });

            var auctionInfo = await SellerNFTMarketContractStub.GetEnglishAuctionInfo.CallAsync(
                new GetEnglishAuctionInfoInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
            auctionInfo.Owner.ShouldBe(DefaultAddress);
            auctionInfo.PurchaseSymbol.ShouldBe("ELF");
            auctionInfo.StartingPrice.ShouldBe(100_00000000);
            auctionInfo.Duration.DurationHours.ShouldBe(100);

            return symbol;
        }

        [Fact]
        public async Task<string> PlaceBidForEnglishAuctionTest()
        {
            var symbol = await ListWithEnglishAuctionTest();

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 90_00000000
                }
            });

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
                offerList.Value.Count.ShouldBe(1);
                offerList.Value.First().From.ShouldBe(User2Address);
                offerList.Value.First().Price.Amount.ShouldBe(90_00000000);
            }

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                }
            });

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
                offerList.Value.Count.ShouldBe(1);
            }

            {
                var bidList = await BuyerNFTMarketContractStub.GetBidList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
                bidList.Value.Count.ShouldBe(1);
            }

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 109_00000000
                }
            });

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
                offerList.Value.Count.ShouldBe(2);
            }

            {
                var bidList = await BuyerNFTMarketContractStub.GetBidList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
                bidList.Value.Count.ShouldBe(1);
            }

            return symbol;
        }

        [Fact]
        public async Task DealToEnglishAuctionTest()
        {
            var symbol = await PlaceBidForEnglishAuctionTest();

            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });

            await SellerNFTMarketContractStub.Deal.SendAsync(new DealInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferFrom = User2Address,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                },
                Quantity = 1
            });

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(1);
            }

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = DefaultAddress
                });
                balance.Balance.ShouldBe(0);
            }
        }

        [Fact]
        public async Task<string> ListWithDutchAuctionTest()
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

            await SellerNFTMarketContractStub.ListWithDutchAuction.SendAsync(new ListWithDutchAuctionInput
            {
                Symbol = symbol,
                TokenId = 233,
                Duration = new ListDuration
                {
                    StartTime = TimestampHelper.GetUtcNow(),
                    DurationHours = 100
                },
                PurchaseSymbol = "ELF",
                StartingPrice = 100_00000000,
                EndingPrice = 50_00000000
            });

            var auctionInfo = await SellerNFTMarketContractStub.GetDutchAuctionInfo.CallAsync(
                new GetDutchAuctionInfoInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
            auctionInfo.Owner.ShouldBe(DefaultAddress);
            auctionInfo.PurchaseSymbol.ShouldBe("ELF");
            auctionInfo.StartingPrice.ShouldBe(100_00000000);
            auctionInfo.EndingPrice.ShouldBe(50_00000000);
            auctionInfo.Duration.DurationHours.ShouldBe(100);

            return symbol;
        }

        [Fact]
        public async Task PlaceBidForDutchAuctionTest()
        {
            var symbol = await ListWithDutchAuctionTest();
            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 49_00000000
                },
                Quantity = 1
            });

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
                offerList.Value.Count.ShouldBe(1);
            }

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
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 100_00000000
                },
                Quantity = 1
            });

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(1);
            }

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = DefaultAddress
                });
                balance.Balance.ShouldBe(0);
            }
        }
    }
}