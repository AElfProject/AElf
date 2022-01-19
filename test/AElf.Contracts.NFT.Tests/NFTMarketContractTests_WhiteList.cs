using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractTests
    {
        [Fact]
        public async Task DealToFixedPriceBuyerInWhiteList()
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
                Quantity = 1,
                WhiteListAddressPriceList = new WhiteListAddressPriceList
                {
                    Value =
                    {
                        new WhiteListAddressPrice
                        {
                            Address = User2Address,
                            Price = new Price
                            {
                                Symbol = "ELF",
                                Amount = 10_00000000
                            }
                        }
                    }
                }
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
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 10_00000000
                },
            });

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Address = User2Address
                });
                offerList.Value.Count.ShouldBe(1);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 10_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // Because of 10/10000 service fee.
                balance.Balance.ShouldBe(InitialELFAmount + 10_00000000 - 10_00000000 / 1000);
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
        public async Task DealToFixedPriceBuyerInWhiteList_HigherPrice()
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
                Quantity = 1,
                WhiteListAddressPriceList = new WhiteListAddressPriceList
                {
                    Value =
                    {
                        new WhiteListAddressPrice
                        {
                            Address = User2Address,
                            Price = new Price
                            {
                                Symbol = "ELF",
                                Amount = 110_00000000
                            }
                        }
                    }
                }
            });

            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });

            var executionResult = await BuyerNFTMarketContractStub.MakeOffer.SendWithExceptionAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 100_00000000
                },
            });
            executionResult.TransactionResult.Error.ShouldContain("too low");

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                },
            });

            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Address = User2Address
                });
                offerList.Value.Count.ShouldBe(1);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 110_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // Because of 10/10000 service fee.
                balance.Balance.ShouldBe(InitialELFAmount + 110_00000000 - 110_00000000 / 1000);
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