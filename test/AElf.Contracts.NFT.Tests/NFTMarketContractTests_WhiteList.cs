using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using QuickGraph;
using Shouldly;
using Xunit;
using InitializeInput = AElf.Contracts.NFTMarket.InitializeInput;
using PriceTag = AElf.Contracts.NFTMarket.PriceTagInfo;
using WhitelistInfo = AElf.Contracts.NFTMarket.WhitelistInfo;

namespace AElf.Contracts.NFT;

[Trait("Category", "NFTContract")]
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
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
                        AddressList = new NFTMarket.AddressList() { Value = { User2Address } },
                        PriceTag = new PriceTagInfo
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
        {
            var whiteListId = (await CreatorNFTMarketContractStub.GetWhitelistId.CallAsync(
                new GetWhitelistIdInput()
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = DefaultAddress
                })).WhitelistId;
            var whitelistIds =
                await WhitelistContractStub.GetWhitelistByManager.CallAsync(NFTMarketContractAddress);
            whitelistIds.WhitelistId.Count.ShouldBe(1);
            whitelistIds.WhitelistId[0].ShouldBe(whiteListId);

            var whitelist = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whiteListId);
            whitelist.Value.Count.ShouldBe(1);
            whitelist.Value[0].AddressList.Value[0].ShouldBe(User2Address);

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
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
                        AddressList = new NFTMarket.AddressList() { Value = { User2Address } },
                        PriceTag = new PriceTagInfo
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
            offerList.Value[0].Quantity.ShouldBe(1);
            offerList.Value[0].Price.Amount.ShouldBe(100_00000000);
            offerList.Value[1].Quantity.ShouldBe(2);
            offerList.Value[1].Price.Amount.ShouldBe(110_00000000);
        }

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
    public async Task DealToFixedPriceBuyerInWhiteList_Complicated()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
                PublicTime = TimestampHelper.GetUtcNow().AddDays(1)
            },
            Quantity = 10,
            Whitelists = new WhitelistInfoList
            {
                Whitelists =
                {
                    new WhitelistInfo
                    {
                        AddressList = new NFTMarket.AddressList { Value = { User2Address } },
                        PriceTag = new PriceTagInfo
                        {
                            TagName = "90 ELF",
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

        var whitelistId = (await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = symbol,
            TokenId = 233,
            Owner = DefaultAddress
        })).WhitelistId;
        var whitelistPrice = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
            new GetExtraInfoByAddressInput
            {
                Address = User2Address,
                WhitelistId = whitelistId
            });
        whitelistPrice.TagName.ShouldBe("90 ELF");

        {
            var whitelistInfo = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelistInfo.ExtraInfoIdList.Value.Single().AddressList.Value.Count.ShouldBe(1);
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

        {
            var whitelistInfo = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
            whitelistInfo.ExtraInfoIdList.Value.Single().AddressList.Value.Count.ShouldBe(0);
        }

        await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
        {
            Symbol = symbol,
            TokenId = 233,
            OfferTo = DefaultAddress,
            Quantity = 3,
            Price = new Price
            {
                Symbol = "ELF",
                Amount = 110_00000000
            }
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

        {
            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(2);
            offerList.Value.First().Price.Amount.ShouldBe(101_00000000);
            offerList.Value.First().Quantity.ShouldBe(1);
            offerList.Value.Last().Price.Amount.ShouldBe(110_00000000);
            offerList.Value.Last().Quantity.ShouldBe(3);
        }

        await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
        {
            Symbol = symbol,
            TokenId = 233,
            OfferTo = DefaultAddress,
            Quantity = 1,
            Price = new Price
            {
                Symbol = "ELF",
                Amount = 201_00000000
            },
        });

        {
            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(3);
        }

        await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
        {
            Symbol = symbol,
            TokenId = 233,
            OfferTo = DefaultAddress,
            Quantity = 1,
            Price = new Price
            {
                Symbol = "ELF",
                Amount = 200_00000000
            },
        });

        {
            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(4);
            offerList.Value.Last().Quantity.ShouldBe(1);
            offerList.Value.Last().Price.Amount.ShouldBe(200_00000000);
        }
    }

    [Fact]
    public async Task ListWithFixedPriceWhitelist()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
        var executionResult1 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
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
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User2Address }
                            },
                            PriceTag = new PriceTagInfo
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
        var whitelistId = (await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = symbol,
            TokenId = 233,
            Owner = DefaultAddress
        })).WhitelistId;
        var log = FixedPriceNFTListed.Parser.ParseFrom(executionResult1.TransactionResult.Logs
            .First(l => l.Name == nameof(FixedPriceNFTListed)).NonIndexed);
        log.WhitelistId.Value.ShouldBe(whitelistId);
        log.Quantity.ShouldBe(10);
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


        var executionResult2 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(new ListWithFixedPriceInput
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
                        AddressList = new NFTMarket.AddressList()
                        {
                            Value =
                            {
                                User3Address, User4Address
                            }
                        },
                        PriceTag = new PriceTagInfo
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
                        AddressList = new NFTMarket.AddressList() { Value = { User5Address } },
                        PriceTag = new PriceTagInfo
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
        var log1 = FixedPriceNFTListed.Parser.ParseFrom(executionResult2.TransactionResult.Logs
            .First(l => l.Name == nameof(FixedPriceNFTListed)).NonIndexed);
        log1.Quantity.ShouldBe(10);
        
        var projectId = HashHelper.ComputeFrom($"{symbol}{233}{DefaultAddress}");
        var tagInfoId = HashHelper.ComputeFrom($"{whitelistId}{projectId}90_00000000 ELF");
        var ifExist = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
            new GetAddressFromWhitelistInput()
            {
                WhitelistId = whitelistId,
                Address = User2Address
            });
        ifExist.Value.ShouldBe(false);

        var extraInfoList = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
        extraInfoList.ExtraInfoIdList.Value.Count.ShouldBe(2);

        var tagIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
        {
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
        tagInfo.AddressList.Value.Count.ShouldBe(2);

        var whitelistPrice = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
            new GetExtraInfoByAddressInput()
            {
                Address = User3Address,
                WhitelistId = whitelistId
            });
        whitelistPrice.TagName.ShouldBe("90_00000000 ELF");
    }

    [Fact]
    public async Task ListWithFixedPriceWhitelist_whitelistFirstNull()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
        var executionResult1 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
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
                IsMergeToPreviousListedInfo = true,
                IsWhitelistAvailable = true
            });
        var whitelistId = (await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = symbol,
            TokenId = 233,
            Owner = DefaultAddress
        })).WhitelistId;
        var log = FixedPriceNFTListed.Parser.ParseFrom(executionResult1.TransactionResult.Logs
            .First(l => l.Name == nameof(FixedPriceNFTListed)).NonIndexed).WhitelistId;
        log.Value.ShouldBe(whitelistId);
        var extraInfo = await WhitelistContractStub.GetWhitelistDetail.CallAsync(whitelistId);
        extraInfo.Value.Count.ShouldBe(0);
        {
            var ifExist1 = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                new GetAddressFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Address = User2Address
                });
            ifExist1.Value.ShouldBe(false);
        }

        await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
        {
            Symbol = "ELF",
            Amount = long.MaxValue,
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
                        AddressList = new NFTMarket.AddressList()
                        {
                            Value =
                            {
                                User3Address, User4Address
                            }
                        },
                        PriceTag = new PriceTagInfo
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
                        AddressList = new NFTMarket.AddressList() { Value = { User5Address } },
                        PriceTag = new PriceTagInfo
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
        var tagInfoId = HashHelper.ComputeFrom($"{whitelistId}{projectId}90_00000000 ELF");
        var ifExist = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
            new GetAddressFromWhitelistInput()
            {
                WhitelistId = whitelistId,
                Address = User3Address
            });
        ifExist.Value.ShouldBe(true);

        var extraInfoList = await WhitelistContractStub.GetWhitelist.CallAsync(whitelistId);
        extraInfoList.ExtraInfoIdList.Value.Count.ShouldBe(2);

        var tagIdList = await WhitelistContractStub.GetExtraInfoIdList.CallAsync(new GetExtraInfoIdListInput()
        {
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
        tagInfo.AddressList.Value.Count.ShouldBe(2);

        var whitelistPrice = await WhitelistContractStub.GetExtraInfoByAddress.CallAsync(
            new GetExtraInfoByAddressInput()
            {
                Address = User3Address,
                WhitelistId = whitelistId
            });
        whitelistPrice.TagName.ShouldBe("90_00000000 ELF");
    }

    [Fact]
    public async Task ListWithFixedPriceWhitelist_NoWhitelistContract()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
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
        var executionResult1 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendWithExceptionAsync(
            new ListWithFixedPriceInput
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
                IsMergeToPreviousListedInfo = true,
                IsWhitelistAvailable = true
            });
        executionResult1.TransactionResult.Error.ShouldContain("Whitelist Contract not initialized.");
    }
    
    [Fact]
    public async Task MakeOffer_Whitelist_Affordable()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = InitialELFAmount,
            To = User3Address,
        });
        await NFTContractStub.Approve.SendAsync(new ApproveInput
        {
            Symbol = symbol,
            TokenId = 233,
            Amount = 20,
            Spender = NFTMarketContractAddress
        });
        var executionResult1 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
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
                Quantity = 5,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User2Address }
                            },
                            PriceTag = new PriceTagInfo
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
        var executionResult2 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                Duration = new ListDuration
                {
                    DurationHours = 24,
                    //PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User3Address }
                            },
                            PriceTag = new PriceTagInfo
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
        var whitelistId = (await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput()
        {
            Symbol = symbol,
            TokenId = 233,
            Owner = DefaultAddress
        })).WhitelistId;
        var log = FixedPriceNFTListed.Parser.ParseFrom(executionResult1.TransactionResult.Logs
            .First(l => l.Name == nameof(FixedPriceNFTListed)).NonIndexed).WhitelistId;
        log.Value.ShouldBe(whitelistId);
        {
            var ifExist1 = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                new GetAddressFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Address = User2Address
                });
            ifExist1.Value.ShouldBe(true);
        }
        {
            var ifExist2 = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                new GetAddressFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Address = User3Address
                });
            ifExist2.Value.ShouldBe(true);
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
                Amount = 200_00000000
            },
        });
        await NFTBuyer2TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
        {
            Symbol = "ELF",
            Amount = long.MaxValue,
            Spender = NFTMarketContractAddress
        });
        await Buyer2NFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
        {
            Symbol = symbol,
            TokenId = 233,
            OfferTo = DefaultAddress,
            Quantity = 3,
            Price = new Price
            {
                Symbol = "ELF",
                Amount = 200_00000000
            },
        });
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
            {
                Symbol = "ELF",
                Owner = User2Address
            });
            balance.Balance.ShouldBe(InitialELFAmount - 90_00000000 - 200_00000000);
        }
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
            {
                Symbol = "ELF",
                Owner = User3Address
            });
            balance.Balance.ShouldBe(InitialELFAmount - 110_00000000 - 200_00000000 - 200_00000000);
        }
        {
            var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = User2Address
            });
            nftBalance.Balance.ShouldBe(2);
        }
        {
            var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = User3Address
            });
            nftBalance.Balance.ShouldBe(3);
        }
        {
            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(0);
        }
    }
    [Fact]
    public async Task MakeOfferTest_WithWhitelist_ToOfferList()
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });

            await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialELFAmount,
                To = User3Address,
            });

            await NFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = symbol,
                TokenId = 233,
                Amount = 100,
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
                Quantity = 5,
                IsWhitelistAvailable = true,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User2Address }
                            },
                            PriceTag = new PriceTagInfo
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
                IsMergeToPreviousListedInfo = false
            });
            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            var executionResult1 = await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 90_00000000
                }
            });
            await NFTBuyer2TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            var executionResult2 = await Buyer2NFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 5,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                }
            });
            var quantity = ListedNFTChanged.Parser.ParseFrom(executionResult1.TransactionResult.Logs
                .First(l => l.Name == nameof(ListedNFTChanged)).NonIndexed).Quantity;
            quantity.ShouldBe(4);
            var log = ListedNFTRemoved.Parser.ParseFrom(executionResult2.TransactionResult.Logs
                .First(l => l.Name == nameof(ListedNFTRemoved)).NonIndexed);
            log.Price.Amount.ShouldBe(100_00000000);
            log.Symbol.ShouldBe(symbol);
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
                    Owner = User3Address
                });
                nftBalance.Balance.ShouldBe(4);
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
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User3Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 400_00000000);
            }
            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Address = User2Address
                });
                offerList.Value.Count.ShouldBe(1);
                offerList.Value[0].Price.Amount.ShouldBe(90_00000000);
            }
            {
                var offerList = await Buyer2NFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Address = User3Address
                });
                offerList.Value.Count.ShouldBe(1);
                offerList.Value[0].Price.Amount.ShouldBe(110_00000000);
            }
        }
    
    [Fact]
    public async Task MakeOffer_NotSetPublicTime()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = InitialELFAmount,
            To = User3Address,
        });
        await NFTContractStub.Approve.SendAsync(new ApproveInput
        {
            Symbol = symbol,
            TokenId = 233,
            Amount = 20,
            Spender = NFTMarketContractAddress
        });
        var executionResult1 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
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
                    //PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 5,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User2Address }
                            },
                            PriceTag = new PriceTagInfo
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
        var executionResult2 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                Duration = new ListDuration
                {
                    DurationHours = 24,
                    //PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User3Address }
                            },
                            PriceTag = new PriceTagInfo
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
                Amount = 90_00000000
            },
        });
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
            {
                Symbol = "ELF",
                Owner = User2Address
            });
            balance.Balance.ShouldBe(InitialELFAmount - 90_00000000 - 100_00000000);
        }
        {
            var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = User2Address
            });
            nftBalance.Balance.ShouldBe(2);
        }
        await NFTBuyer2TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
        {
            Symbol = "ELF",
            Amount = long.MaxValue,
            Spender = NFTMarketContractAddress
        });
        await Buyer2NFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
        {
            Symbol = symbol,
            TokenId = 233,
            OfferTo = DefaultAddress,
            Quantity = 3,
            Price = new Price
            {
                Symbol = "ELF",
                Amount = 200_00000000
            },
        });
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
            {
                Symbol = "ELF",
                Owner = User3Address
            });
            balance.Balance.ShouldBe(InitialELFAmount - 110_00000000 - 100_00000000 - 100_00000000);
        }
        {
            var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = User3Address
            });
            nftBalance.Balance.ShouldBe(3);
        }
        {
            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(1);
            offerList.Value[0].Quantity.ShouldBe(2);
        }
    }

    [Fact]
    public async Task MakeOfferTest_NotInWhitelist()
    {
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });
            await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialELFAmount,
                To = User3Address,
            });

            await NFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = symbol,
                TokenId = 233,
                Amount = 20,
                Spender = NFTMarketContractAddress
            });
            await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
                new ListWithFixedPriceInput
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
                    Quantity = 10,
                    Whitelists = new WhitelistInfoList()
                    {
                        Whitelists =
                        {
                            new WhitelistInfo()
                            {
                                AddressList = new NFTMarket.AddressList
                                {
                                    Value = { User3Address }
                                },
                                PriceTag = new PriceTagInfo
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

            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            var executionResult1 = await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
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
            var quantity = ListedNFTChanged.Parser.ParseFrom(executionResult1.TransactionResult.Logs
                .First(l => l.Name == nameof(ListedNFTChanged)).NonIndexed).Quantity;
            quantity.ShouldBe(8);
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 200_00000000);
            }
            {
                var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Owner = User2Address
                });
                nftBalance.Balance.ShouldBe(2);
            }
            await NFTBuyer2TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            var executionResult2 = await Buyer2NFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 90_00000000
                },
            });
            var quantity1 = ListedNFTChanged.Parser.ParseFrom(executionResult2.TransactionResult.Logs
                .First(l => l.Name == nameof(ListedNFTChanged)).NonIndexed).Quantity;
            quantity1.ShouldBe(7);
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User3Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 90_00000000);
            }
        }
    }
    
    [Fact]
    public async Task MakeOfferTest_WithPublicTime()
        {
            await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                ServiceFeeReceiver = MarketServiceFeeReceiverAddress
            });

            await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialELFAmount,
                To = User3Address,
            });

            await NFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = symbol,
                TokenId = 233,
                Amount = 100,
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
                Quantity = 5,
                IsWhitelistAvailable = true,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = { User2Address }
                            },
                            PriceTag = new PriceTagInfo
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
                IsMergeToPreviousListedInfo = false
            });
            await NFTBuyerTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            var executionResult1 = await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 90_00000000
                }
            });
            await NFTBuyer2TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Symbol = "ELF",
                Amount = long.MaxValue,
                Spender = NFTMarketContractAddress
            });
            var executionResult2 = await Buyer2NFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 233,
                OfferTo = DefaultAddress,
                Quantity = 5,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 110_00000000
                }
            });
            var quantity = ListedNFTChanged.Parser.ParseFrom(executionResult1.TransactionResult.Logs
                .First(l => l.Name == nameof(ListedNFTChanged)).NonIndexed).Quantity;
            quantity.ShouldBe(4);
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
                    Owner = User3Address
                });
                nftBalance.Balance.ShouldBe(0);
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
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User3Address
                });
                balance.Balance.ShouldBe(InitialELFAmount);
            }
            {
                var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Address = User2Address
                });
                offerList.Value.Count.ShouldBe(1);
                offerList.Value[0].Price.Amount.ShouldBe(90_00000000);
            }
            {
                var offerList = await Buyer2NFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
                {
                    Symbol = symbol,
                    TokenId = 233,
                    Address = User3Address
                });
                offerList.Value.Count.ShouldBe(1);
                offerList.Value[0].Quantity.ShouldBe(5);
                offerList.Value[0].Price.Amount.ShouldBe(110_00000000);
            }
        }

    [Fact]
    public async Task<string> CreateBadgeTest_new()
    {
        await BuyerNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await BuyerNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);
        var createWhitelistInput = new CreateWhitelistInput
        {
            ProjectId = HashHelper.ComputeFrom("Badge Test"),
            Creator = NFTMarketContractAddress,
            ExtraInfoList = new ExtraInfoList()
            {
                Value =
                {
                    new ExtraInfo()
                    {
                        AddressList = new Whitelist.AddressList
                        {
                            Value = { User1Address, User2Address }
                        }
                    }
                }
            },
            IsCloneable = true,
            StrategyType = StrategyType.Basic,
            Remark = "Badge Test"
        };
        var createWhitelistResult = await WhitelistContractStub.CreateWhitelist.SendAsync(createWhitelistInput);
        var whitelistId = createWhitelistResult.Output;

        var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
        {
            BaseUri = BaseUri,
            Creator = DefaultAddress,
            IsBurnable = true,
            Metadata = new Metadata
            {
                Value =
                {
                    { "Description", "Stands for the human race." }
                }
            },
            NftType = NFTType.Badges.ToString(),
            ProtocolName = "Badge",
            IsTokenIdReuse = true,
            MinterList = new MinterList
            {
                Value = { NFTMarketContractAddress }
            },
            TotalSupply = 1_000_000_000 // One billion
        });
        var symbol = executionResult.Output.Value;
        await NFTContractStub.Mint.SendAsync(new MintInput
        {
            Symbol = symbol,
            Alias = "badge",
            Metadata = new Metadata
            {
                Value =
                {
                    { "Special Property", "A Value" },
                    { "aelf_badge_whitelist", whitelistId.ToHex() }
                }
            },
            Owner = DefaultAddress,
            Uri = $"{BaseUri}foo"
        });
        await BuyerNFTMarketContractStub.MintBadge.SendAsync(new MintBadgeInput()
        {
            Symbol = symbol,
            TokenId = 1
        });
        var exception = await Buyer2NFTMarketContractStub.MintBadge.SendWithExceptionAsync(new MintBadgeInput()
        {
            Symbol = symbol,
            TokenId = 1
        });
        exception.TransactionResult.Error.ShouldContain("No permission.");
        {
            var ifExist = await WhitelistContractStub.GetAddressFromWhitelist.CallAsync(
                new GetAddressFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    Address = User2Address
                });
            ifExist.Value.ShouldBe(false);
        }

        var userBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = symbol,
            TokenId = 1,
            Owner = User2Address
        });
        userBalance.Balance.ShouldBe(1);
        return symbol;
    }

    [Fact]
    public async Task<string> CreateBadgeTest_new_whitelistIdNull()
    {
        await BuyerNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await BuyerNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

        var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
        {
            BaseUri = BaseUri,
            Creator = DefaultAddress,
            IsBurnable = true,
            Metadata = new Metadata
            {
                Value =
                {
                    { "Description", "Stands for the human race." }
                }
            },
            NftType = NFTType.Badges.ToString(),
            ProtocolName = "Badge",
            TotalSupply = 1_000_000_000 // One billion
        });
        var symbol = executionResult.Output.Value;
        await NFTContractStub.Mint.SendAsync(new MintInput
        {
            Symbol = symbol,
            Alias = "badge",
            Metadata = new Metadata
            {
                Value =
                {
                    { "Special Property", "A Value" },
                    { "aelf_badge_whitelist", new Hash().ToHex() }
                }
            },
            Owner = DefaultAddress,
            Uri = $"{BaseUri}foo"
        });
        var executionResult1 = await BuyerNFTMarketContractStub.MintBadge.SendWithExceptionAsync(new MintBadgeInput()
        {
            Symbol = symbol,
            TokenId = 1
        });
        executionResult1.TransactionResult.Error.ShouldContain("No whitelist.");
        return symbol;
    }

    [Fact]
    public async Task List_Duplicate()
    {
        await AdminNFTMarketContractStub.Initialize.SendAsync(new InitializeInput
        {
            NftContractAddress = NFTContractAddress,
            ServiceFeeReceiver = MarketServiceFeeReceiverAddress
        });
        await AdminNFTMarketContractStub.SetWhitelistContract.SendAsync(WhitelistContractAddress);

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
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = InitialELFAmount,
            To = User3Address,
        });

        await NFTContractStub.Approve.SendAsync(new ApproveInput
        {
            Symbol = symbol,
            TokenId = 233,
            Amount = 20,
            Spender = NFTMarketContractAddress
        });
        var executionResult1 = await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 5_00000000
                },
                Duration = new ListDuration
                {
                    DurationHours = 24,
                    //PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            AddressList = new NFTMarket.AddressList
                            {
                                Value = {User2Address}
                            },
                            PriceTag = new PriceTagInfo
                            {
                                TagName = "1_00000000 ELF",
                                Price = new Price
                                {
                                    Symbol = "ELF",
                                    Amount = 1_00000000
                                }
                            }
                        }
                    }
                },
                IsWhitelistAvailable = true
            });
        var whitelistId = (await SellerNFTMarketContractStub.GetWhitelistId.CallAsync(new GetWhitelistIdInput
        {
            Symbol = symbol,
            TokenId = 233,
            Owner = DefaultAddress
        })).WhitelistId;
        var log = ListedNFTAdded.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTAdded)).NonIndexed)
            .WhitelistId;
        log.ShouldBe(whitelistId);
        await SellerNFTMarketContractStub.ListWithFixedPrice.SendAsync(
            new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 10_00000000
                },
                Duration = new ListDuration
                {
                    DurationHours = 24,
                    //PublicTime = TimestampHelper.GetUtcNow().AddDays((1))
                },
                Quantity = 10,
                IsWhitelistAvailable = true,
                IsMergeToPreviousListedInfo = false
            });
        var log1 = ListedNFTAdded.Parser
            .ParseFrom(executionResult1.TransactionResult.Logs.First(l => l.Name == nameof(ListedNFTAdded)).NonIndexed)
            .WhitelistId;
        log1.ShouldBe(whitelistId);
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
            Quantity = 5,
            Price = new Price
            {
                Symbol = "ELF",
                Amount = 10_00000000
            },
        });
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
            {
                Symbol = "ELF",
                Owner = User2Address
            });
            balance.Balance.ShouldBe(InitialELFAmount - 1_00000000 - 5_00000000 * 4);
        }
        {
            var offerList = await BuyerNFTMarketContractStub.GetOfferList.CallAsync(new GetOfferListInput
            {
                Symbol = symbol,
                TokenId = 233,
                Address = User2Address
            });
            offerList.Value.Count.ShouldBe(0);
        }
    }
}