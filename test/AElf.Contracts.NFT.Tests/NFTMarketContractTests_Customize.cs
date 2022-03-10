using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.NFTMinter;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using InitializeInput = AElf.Contracts.NFTMarket.InitializeInput;

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
                TokenId = 2,
                OfferTo = DefaultAddress,
                Quantity = 1,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                ExpireTime = TimestampHelper.GetUtcNow().AddMinutes(30)
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 40_00000000);
            }

            var requestInfo = await BuyerNFTMarketContractStub.GetRequestInfo.CallAsync(new GetRequestInfoInput
            {
                Symbol = symbol,
                TokenId = 2
            });
            requestInfo.DepositRate.ShouldBe(2000);
            requestInfo.IsConfirmed.ShouldBeFalse();
            return symbol;
        }

        [Fact]
        public async Task<string> ConfirmRequestTest()
        {
            var symbol = await RequestNewNFTTest();
            await CreatorNFTMarketContractStub.HandleRequest.SendAsync(new HandleRequestInput
            {
                Symbol = symbol,
                TokenId = 2,
                IsConfirm = true,
                Requester = User2Address
            });

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // Received 50% deposit, but need to sub 10_00000000 staking tokens and 2000000 service fees.
                balance.Balance.ShouldBe(InitialELFAmount + 20_00000000 - 10_00000000 - 2000000);
            }

            var requestInfo = await BuyerNFTMarketContractStub.GetRequestInfo.CallAsync(new GetRequestInfoInput
            {
                Symbol = symbol,
                TokenId = 2
            });
            requestInfo.IsConfirmed.ShouldBeTrue();
            return symbol;
        }

        [Fact]
        public async Task<string> ListForRequestedNFTTest()
        {
            var symbol = await ConfirmRequestTest();

            // Need to mint this token id first.
            await NFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                TokenId = 2,
                Quantity = 1,
                Alias = "Gift"
            });
            await NFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = symbol,
                TokenId = 2,
                Amount = 1,
                Spender = NFTMarketContractAddress
            });

            {
                var executionResult = await CreatorNFTMarketContractStub.ListWithEnglishAuction.SendWithExceptionAsync(
                    new ListWithEnglishAuctionInput
                    {
                        Symbol = symbol,
                        TokenId = 2,
                        StartingPrice = 100_00000000,
                        PurchaseSymbol = "ELF",
                        Duration = new ListDuration
                        {
                            StartTime = TimestampHelper.GetUtcNow(),
                            PublicTime = TimestampHelper.GetUtcNow(),
                            DurationHours = 7
                        }
                    });
                executionResult.TransactionResult.Error.ShouldContain(
                    "This NFT cannot be listed with auction for now.");
            }

            {
                var executionResult = await CreatorNFTMarketContractStub.ListWithDutchAuction.SendWithExceptionAsync(
                    new ListWithDutchAuctionInput
                    {
                        Symbol = symbol,
                        TokenId = 2,
                        StartingPrice = 100_00000000,
                        EndingPrice = 50_00000000,
                        PurchaseSymbol = "ELF",
                        Duration = new ListDuration
                        {
                            StartTime = TimestampHelper.GetUtcNow(),
                            PublicTime = TimestampHelper.GetUtcNow(),
                            DurationHours = 7
                        }
                    });
                executionResult.TransactionResult.Error.ShouldContain(
                    "This NFT cannot be listed with auction for now.");
            }

            var listInput = new ListWithFixedPriceInput
            {
                Symbol = symbol,
                TokenId = 2,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                Duration = new ListDuration
                {
                    StartTime = TimestampHelper.GetUtcNow(),
                    PublicTime = TimestampHelper.GetUtcNow().AddHours(11),
                    DurationHours = 100
                },
                Quantity = 1,
            };
            
            {
                var executionResult =
                    await CreatorNFTMarketContractStub.ListWithFixedPrice.SendWithExceptionAsync(listInput);
                executionResult.TransactionResult.Error.ShouldContain("Incorrect white list address price list.");
            }

            listInput.Price.Amount = 100_00000000;
            listInput.WhiteListAddressPriceList = new WhiteListAddressPriceList
            {
                Value =
                {
                    new WhiteListAddressPrice
                    {
                        Address = User2Address,
                        Price = new Price
                        {
                            Symbol = "ELF",
                            Amount = 200_00000000
                        }
                    }
                }
            };

            {
                var executionResult =
                    await CreatorNFTMarketContractStub.ListWithFixedPrice.SendWithExceptionAsync(listInput);
                executionResult.TransactionResult.Error.ShouldContain("too low");
            }

            listInput.Price.Amount = 200_00000000;
            await CreatorNFTMarketContractStub.ListWithFixedPrice.SendAsync(listInput);

            var requestInfo = await CreatorNFTMarketContractStub.GetRequestInfo.CallAsync(new GetRequestInfoInput
            {
                Symbol = symbol,
                TokenId = 2
            });
            requestInfo.ListTime.ShouldNotBeNull();

            var whiteListPriceList = await CreatorNFTMarketContractStub.GetWhiteListAddressPriceList.CallAsync(
                new GetWhiteListAddressPriceListInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = DefaultAddress
                });
            whiteListPriceList.Value.Count.ShouldBe(1);
            whiteListPriceList.Value[0].Address.ShouldBe(User2Address);
            whiteListPriceList.Value[0].Price.Symbol.ShouldBe("ELF");
            // Instead of 200_00000000.
            whiteListPriceList.Value[0].Price.Amount.ShouldBe(160_00000000);
            return symbol;
        }

        [Fact]
        public async Task BuyRequestedNFTTest()
        {
            var symbol = await ListForRequestedNFTTest();

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 40_00000000);
            }

            await BuyerNFTMarketContractStub.MakeOffer.SendAsync(new MakeOfferInput
            {
                Symbol = symbol,
                TokenId = 2,
                OfferTo = DefaultAddress,
                Price = new Price
                {
                    Symbol = "ELF",
                    Amount = 160_00000000
                },
                Quantity = 1
            });

            var requestInfo = await CreatorNFTMarketContractStub.GetRequestInfo.CallAsync(new GetRequestInfoInput
            {
                Symbol = symbol,
                TokenId = 2
            });
            // Removed after dealing.
            requestInfo.Price.ShouldBeNull();

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = User2Address
                });
                balance.Balance.ShouldBe(InitialELFAmount - 200_00000000);
            }

            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new MultiToken.GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = DefaultAddress
                });
                // NFT price 200 ELF - Staking 10 ELF - 0.2 Service Fee ELF.
                balance.Balance.ShouldBe(InitialELFAmount + 200_00000000 - 10_00000000 - 20000000);
            }

            {
                var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = User2Address
                });
                nftBalance.Balance.ShouldBe(1);
            }

            {
                var nftBalance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = symbol,
                    TokenId = 2,
                    Owner = DefaultAddress
                });
                nftBalance.Balance.ShouldBe(0);
            }
        }
    }
}