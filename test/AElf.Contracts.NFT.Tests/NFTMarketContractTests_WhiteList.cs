using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf;
using Shouldly;
using Xunit;
using InitializeInput = AElf.Contracts.NFTMarket.InitializeInput;
using PriceTag = AElf.Contracts.NFTMarket.PriceTag;
using WhitelistInfo = AElf.Contracts.NFTMarket.WhitelistInfo;

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
                WhitelistContractAddress = WhitelistContractAddress,
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
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists = { new WhitelistInfo()
                    {
                        Address = User2Address,
                        PriceTag = new PriceTag()
                        {
                            TagName = "10_00000000 ELF",
                            Price = new Price
                            {
                                Symbol = "ELF",
                                Amount = 10_00000000
                            }
                        }
                    } }
                },
                IsWhitelistAvailable = true
            });
            {
                var whiteListId = await CreatorNFTMarketContractStub.GetWhitelistId.CallAsync(
                    new GetWhitelistIdInput()
                    {
                        Symbol = symbol,
                        TokenId = 233,
                        Owner = DefaultAddress
                    });
                var whitelistIds = await WhitelistContractStub.GetWhitelistByManager.CallAsync(NFTMarketContractAddress);
                whitelistIds.WhitelistId.Count.ShouldBe(1);
                whitelistIds.WhitelistId[0].ShouldBe(whiteListId);
                
                var whitelist = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whiteListId); 
                whitelist.Value.Count.ShouldBe(1);
                whitelist.Value[0].Address.ShouldBe(User2Address);
                
                var price = new Price();
                price.MergeFrom(whitelist.Value[0].Info.Info);
                price.Symbol.ShouldBe("ELF");
                price.Amount.ShouldBe(10_00000000);
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
                WhitelistContractAddress = WhitelistContractAddress,
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
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            Address = User2Address,
                            PriceTag = new PriceTag()
                            {
                                TagName = "110_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 110_00000000
                                }
                            }
                        }
                    }
                },
                IsWhitelistAvailable = true
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
                    Amount = 100_00000000
                },
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
                offerList.Value.Count.ShouldBe(2);
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

        [Fact]
        public async Task DealToFixedPriceBuyerInWhiteList_Complicated()
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                WhitelistContractAddress = WhitelistContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });

            var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
            {
                ProtocolName = "aelf Collections",
                NftType = NFTType.Collectables.ToString(),
                TotalSupply = 1000,
                IsBurnable = false,
                IsTokenIdReuse = true
            });
            var symbol = executionResult.Output.Value;

            await NFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                Alias = "test",
                Quantity = 10,
                TokenId = 233
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
                TokenId = 233,
                Amount = 10,
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
                    DurationHours = 24,
                    PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            Address = User2Address,
                            PriceTag = new PriceTag()
                            {
                                TagName = "90_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 90_00000000
                                }
                            }
                        }
                    }
                },
                IsWhitelistAvailable = true
            });
           
            var whitelistId = await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
                {
                    Owner = DefaultAddress,
                    Symbol = symbol,
                    TokenId = 233
                });
            var whitelistPrice = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
                    new GetExtraInfoByAddressInput()
                    {
                        Address = User2Address,
                        WhitelistId = whitelistId
                    });
            whitelistPrice.TagName.ShouldBe("90_00000000 ELF");
                
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
                    Amount = 101_00000000
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
                offerList.Value.First().Price.Amount.ShouldBe(101_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 90_00000000);
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
                nftBalance.Balance.ShouldBe(9);
            }
            var whitelistInfo = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelistInfo.ExtraInfoIdList.Value.Count.ShouldBe(0);

            var exceptionAsync = await BuyerNFTMarketContractStub.MakeOffer.SendWithExceptionAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 3,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                },
            });
            exceptionAsync.TransactionResult.Error.ShouldContain("No Match tagInfo according to the address");

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 90_00000000 );
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

            // {
            //     var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            //     {
            //         Symbol = symbol,
            //         TokenId = 233,
            //         Address = User2Address
            //     });
            //     offerList.Value.Count.ShouldBe(2);
            //     offerList.Value.Last().Price.Amount.ShouldBe(110_00000000);
            //     offerList.Value.Last().Quantity.ShouldBe(2);
            // }
            //
            // await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            // {
            //     Symbol = symbol,
            //     TokenId = 233,
            //     OfferTo = DefaultAddress,
            //     Quantity = 1,
            //     Price = new Price
            //     {
            //         Symbol = "ELF",
            //         Amount = 201_00000000
            //     },
            // });
            //
            // {
            //     var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            //     {
            //         Symbol = symbol,
            //         TokenId = 233,
            //         Address = User2Address
            //     });
            //     offerList.Value.Count.ShouldBe(3);
            // }
            //
            // await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            // {
            //     Symbol = symbol,
            //     TokenId = 233,
            //     OfferTo = DefaultAddress,
            //     Quantity = 1,
            //     Price = new Price
            //     {
            //         Symbol = "ELF",
            //         Amount = 200_00000000
            //     },
            // });
            //
            // {
            //     var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            //     {
            //         Symbol = symbol,
            //         TokenId = 233,
            //         Address = User2Address
            //     });
            //     offerList.Value.Count.ShouldBe(3);
            //     offerList.Value.Last().Quantity.ShouldBe(2);
            // }
        }

        [Fact]
        public async Task ListedWithFixedPriceWhitelist()
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                WhitelistContractAddress = WhitelistContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });

            var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
            {
                ProtocolName = "aelf Collections",
                NftType = NFTType.Collectables.ToString(),
                TotalSupply = 1000,
                IsBurnable = false,
                IsTokenIdReuse = true
            });
            var symbol = executionResult.Output.Value;

            await NFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                Alias = "test",
                Quantity = 20,
                TokenId = 233
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
                TokenId = 233,
                Amount = 20,
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
                    DurationHours = 24,
                    PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            Address = User2Address,
                            PriceTag = new PriceTag()
                            {
                                TagName = "90_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 90_00000000
                                }
                            }
                        }
                    }
                },
                IsWhitelistAvailable = true
            });
            var whitelistId = await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
                {
                    Owner = DefaultAddress,
                    Symbol = symbol,
                    TokenId = 233
                });
            {
                var ifExist1 = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                new GetAddressFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Address = User2Address
                });
                ifExist1.Value.ShouldBe(true);
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
                OfferTo = DefaultAddress,
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 101_00000000
                },
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
                var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = User2Address
                });
                nftBalance.Balance.ShouldBe(1);
            }
            

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
                    DurationHours = 24,
                    PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            Address = User3Address,
                            PriceTag = new PriceTag()
                            {
                                TagName = "90_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 90_00000000
                                }
                            }
                        },
                        new WhitelistInfo()
                        {
                            Address = User4Address,
                            PriceTag = new PriceTag()
                            {
                                TagName = "90_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 90_00000000
                                }
                            }
                        },
                        new WhitelistInfo()
                        {
                            Address = User5Address,
                            PriceTag = new PriceTag()
                            {
                                TagName = "10_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 10_00000000
                                }
                            }
                        }
                    }
                },
                IsWhitelistAvailable = true
            });
            
            var projectId = HashHelper.ComputeFrom($"{symbol}{233}{DefaultAddress}");
            var tagInfoId = HashHelper.ComputeFrom($"{NFTMarketContractAddress}{projectId}90_00000000 ELF");
            var ifExist = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                new GetAddressFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Address = User2Address
                });
            ifExist.Value.ShouldBe(false);
            
            var extraInfoList = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            extraInfoList.ExtraInfoIdList.Value.Count.ShouldBe(3);
            
            var tagIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
            {
                Owner = NFTMarketContractAddress,
                ProjectId = projectId,
                WhitelistId = whitelistId
            });
            tagIdList.Value.Count.ShouldBe(2);
            tagIdList.Value[0].ShouldBe(tagInfoId);
            
            var tagInfo = await WhitelistContractStub.GetExtraInfoByTag.CallAsync(new GetExtraInfoByTagInput()
            {
                WhitelistId = whitelistId,
                TagInfoId = tagInfoId
            });
            tagInfo.Value.Count.ShouldBe(2);
            
            var whitelistPrice = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
                new GetExtraInfoByAddressInput()
                {
                    Address = User3Address,
                    WhitelistId = whitelistId
                });
            whitelistPrice.TagName.ShouldBe("90_00000000 ELF");
        }
    }
}