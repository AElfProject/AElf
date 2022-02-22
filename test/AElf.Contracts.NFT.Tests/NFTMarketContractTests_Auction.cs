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

            var symbol = await CreateArtistsTest();

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
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialELFAmount,
                To = User3Address,
            });

            await NFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                TokenId = 2,
                Quantity = 1,
                Alias = "Gift2"
            });

            await NFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = symbol,
                TokenId = 2,
                Amount = 1,
                Spender = NFTMarketContractAddress
            });

            await SellerNFTMarketContractStub.ListWithEnglishAuction.SendAsync(new ListWithEnglishAuctionInput
            {
                Symbol = symbol,
                TokenId = 2,
                Duration = new ListDuration
                {
                    StartTime = TimestampHelper.GetUtcNow(),
                    DurationHours = 100
                },
                PurchaseSymbol = "ELF",
                StartingPrice = 100_00000000,
                EarnestMoney = 10_00000000
            });

            var auctionInfo = await SellerNFTMarketContractStub.GetEnglishAuctionInfo.CallAsync(
                new GetEnglishAuctionInfoInput
                {
                    Symbol = symbol,
                    TokenId = 2
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
                TokenId = 2,
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
                    TokenId = 2
                });
                offerList.Value.Count.ShouldBe(1);
                offerList.Value.First().From.ShouldBe(User2Address);
                offerList.Value.First().Price.Amount.ShouldBe(90_00000000);
            }

            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            await NFTBuyer2TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 2,
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
                    TokenId = 2
                });
                offerList.Value.Count.ShouldBe(1);
            }

            {
                var bidList = await BuyerNFTMarketContractStub.GetBidList.CallAsync(new GetBidListInput
                {
                    Symbol = symbol,
                    TokenId = 2
                });
                bidList.Value.Count.ShouldBe(1);
            }

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 2,
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
                    TokenId = 2
                });
                offerList.Value.Count.ShouldBe(2);
            }

            {
                var bidList = await BuyerNFTMarketContractStub.GetBidList.CallAsync(new GetBidListInput
                {
                    Symbol = symbol,
                    TokenId = 2
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

            await Buyer2NFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 2,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 105_00000000
                }
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 10_00000000);
            }

            await SellerNFTMarketContractStub.Deal.SendAsync(new DealInput
            {
                Symbol = symbol,
                TokenId = 2,
                OfferFrom = User2Address,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                },
                Quantity = 1
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 110_00000000);
            }

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(1);
            }

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = DefaultAddress
                });
                balance.Balance.ShouldBe(0);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User3Address
                });
                balance.Balance.ShouldBe(InitialELFAmount);
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

            var symbol = await CreateArtistsTest();

            await NFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                TokenId = 2,
                Quantity = 1,
                Alias = "Gift2"
            });

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
                TokenId = 2,
                Amount = 1,
                Spender = NFTMarketContractAddress
            });

            await SellerNFTMarketContractStub.ListWithDutchAuction.SendAsync(new ListWithDutchAuctionInput
            {
                Symbol = symbol,
                TokenId = 2,
                Duration = new ListDuration
                {
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
                    TokenId = 2
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
                TokenId = 2,
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
                    TokenId = 2
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
                TokenId = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                Quantity = 1
            });

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(1);
            }

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 2
                });
                offerList.Value.Count.ShouldBe(1);
            }

            {
                var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = DefaultAddress
                });
                balance.Balance.ShouldBe(0);
            }
        }
    }
}