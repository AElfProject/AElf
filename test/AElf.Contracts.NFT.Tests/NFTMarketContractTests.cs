using System.Linq;
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
                Duration = new ListDuration
                {
                    DurationHours = 24
                },
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
            listedNftInfo.Duration.DurationHours.ShouldBe(24);

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
                OfferTo = DefaultAddress,
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
                balance.Balance.ShouldBe(InitialELFAmount + 100_00000000 - 100_00000000 / 1000);
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

        [Fact]
        public async Task<string> MakeOfferToFixedPrice()
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
                OfferTo = DefaultAddress,
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 90_00000000
                },
            });

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 99_00000000
                },
            });

            var offerAddressList = await BuyerNFTMarketContractStub.GetOfferAddressList.CallAsync(
                new GetOfferAddressListInput
                {
                    Symbol = symbol,
                    TokenId = 233
                });
            offerAddressList.Value.ShouldContain(User2Address);

            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(2);
            offerList.Value.First().Quantity.ShouldBe(2);
            offerList.Value.Last().Quantity.ShouldBe(1);

            return symbol;
        }

        [Fact]
        public async Task DealToOfferWhenFixedPrice()
        {
            var symbol = await MakeOfferToFixedPrice();

            // Set royalty.
            await CreatorNFTMarketContractStub.SetRoyalty.SendAsync(new SetRoyaltyInput
            {
                Symbol = symbol,
                TokenId = 233,
                Royalty = 10,
                RoyaltyFeeReceiver = MarketServiceFeeReceiverAddress
            });

            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233
            });
            offerList.Value.Count.ShouldBe(2);

            var offer = offerList.Value.First();
            await SellerNFTMarketContractStub.Deal.SendAsync(new DealInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferFrom = offer.From,
                Quantity = 1,
                Price = offer.Price
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 90_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // Because of 10/10000 service fee.
                balance.Balance.ShouldBe(InitialELFAmount + 90_00000000 - 90_00000000 / 1000 - 90_00000000 / 1000);
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

        [Fact]
        public async Task DealToOfferWhenNotListed()
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
                OfferTo = DefaultAddress,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 1000_00000000
                },
            });

            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233
            });
            offerList.Value.Count.ShouldBe(1);

            var offer = offerList.Value.First();
            await SellerNFTMarketContractStub.Deal.SendAsync(new DealInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferFrom = offer.From,
                Quantity = 1,
                Price = offer.Price
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 1000_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // Because of 10/10000 service fee.
                balance.Balance.ShouldBe(InitialELFAmount + 1000_00000000 - 1000_00000000 / 1000);
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