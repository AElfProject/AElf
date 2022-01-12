using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using GetBalanceInput = AElf.Contracts.NFT.GetBalanceInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty ListWithFixedPrice(ListWithFixedPriceInput input)
        {
            MaybeReceiveRemainDeposit();
            var duration = AdjustListDuration(input.Duration);
            var whiteListAddressPriceList = input.WhiteListAddressPriceList;
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null && (requestInfo.ListTime == null || // Never listed,
                                        requestInfo.ListTime.AddHours(requestInfo.WhiteListHours) >
                                        Context.CurrentBlockTime)
                // or white list hours not passed -> will refresh list time and white list time.
            )
            {
                ListRequestedNFT(input, requestInfo, whiteListAddressPriceList);
            }

            Assert(GetTokenWhiteList(input.Symbol).Value.Contains(input.Price.Symbol),
                $"{input.Price.Symbol} is not in token white list.");

            var listedNftInfoList = State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] ??
                                    new ListedNFTInfoList();
            ListedNFTInfo listedNftInfo;
            if (input.IsMergeToPreviousListedInfo)
            {
                listedNftInfo = listedNftInfoList.Value.FirstOrDefault(i =>
                    i.Price.Symbol == input.Price.Symbol && i.Price.Amount == input.Price.Amount &&
                    i.Owner == Context.Sender);
            }
            else
            {
                listedNftInfo = listedNftInfoList.Value.FirstOrDefault(i =>
                    i.Price.Symbol == input.Price.Symbol && i.Price.Amount == input.Price.Amount &&
                    i.Owner == Context.Sender && i.Duration.StartTime == input.Duration.StartTime &&
                    i.Duration.PublicTime == input.Duration.PublicTime &&
                    i.Duration.DurationHours == input.Duration.DurationHours);
            }

            if (listedNftInfo == null)
            {
                listedNftInfo = new ListedNFTInfo
                {
                    ListType = ListType.FixedPrice,
                    Owner = Context.Sender,
                    Price = input.Price,
                    Quantity = input.Quantity,
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Duration = duration,
                };
                listedNftInfoList.Value.Add(listedNftInfo);
            }
            else
            {
                listedNftInfo.Quantity = listedNftInfo.Quantity.Add(input.Quantity);
            }

            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, listedNftInfo.Quantity);

            State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfoList;
            if (whiteListAddressPriceList != null)
            {
                State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId] = whiteListAddressPriceList;
            }

            ClearBids(input.Symbol, input.TokenId);
            State.EnglishAuctionInfoMap[input.Symbol].Remove(input.TokenId);
            State.DutchAuctionInfoMap[input.Symbol].Remove(input.TokenId);

            Context.Fire(new ListedNFTInfoChanged
            {
                ListType = listedNftInfo.ListType,
                Owner = listedNftInfo.Owner,
                Price = listedNftInfo.Price,
                Quantity = listedNftInfo.Quantity,
                Symbol = listedNftInfo.Symbol,
                TokenId = listedNftInfo.TokenId,
                Duration = listedNftInfo.Duration
            });

            return new Empty();
        }

        public override Empty ListWithEnglishAuction(ListWithEnglishAuctionInput input)
        {
            Assert(CanBeListedWithAuction(input.Symbol, input.TokenId),
                "This NFT cannot be listed with auction for now.");
            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, 1);
            Assert(input.EarnestMoney <= input.StartingPrice, "Earnest money too high.");
            MaybeReceiveRemainDeposit();

            Assert(GetTokenWhiteList(input.Symbol).Value.Contains(input.PurchaseSymbol),
                $"{input.PurchaseSymbol} is not in token white list.");
            Assert(
                State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput {Symbol = input.PurchaseSymbol}).Symbol !=
                null, $"Token {input.PurchaseSymbol} not support purchase for auction.");

            ClearBids(input.Symbol, input.TokenId);

            var duration = AdjustListDuration(input.Duration);

            var englishAuctionInfo = new EnglishAuctionInfo
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = duration,
                PurchaseSymbol = input.PurchaseSymbol,
                StartingPrice = input.StartingPrice,
                Owner = Context.Sender,
                EarnestMoney = input.EarnestMoney
            };
            State.EnglishAuctionInfoMap[input.Symbol][input.TokenId] = englishAuctionInfo;

            var whiteListAddressPriceList = input.WhiteListAddressPriceList;
            if (whiteListAddressPriceList != null)
            {
                State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId] = whiteListAddressPriceList;
            }

            State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] = new ListedNFTInfoList
            {
                Value =
                {
                    new ListedNFTInfo
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        Duration = duration,
                        ListType = ListType.EnglishAuction,
                        Owner = Context.Sender,
                        Price = new Price
                        {
                            Symbol = input.PurchaseSymbol,
                            Amount = input.StartingPrice
                        },
                        Quantity = 1
                    }
                }
            };

            State.DutchAuctionInfoMap[input.Symbol].Remove(input.TokenId);

            Context.Fire(new ListedNFTInfoChanged
            {
                ListType = ListType.EnglishAuction,
                Owner = englishAuctionInfo.Owner,
                Price = new Price
                {
                    Symbol = input.PurchaseSymbol,
                    Amount = input.StartingPrice
                },
                Quantity = 1,
                Symbol = englishAuctionInfo.Symbol,
                TokenId = englishAuctionInfo.TokenId,
                Duration = englishAuctionInfo.Duration
            });

            return new Empty();
        }

        public override Empty ListWithDutchAuction(ListWithDutchAuctionInput input)
        {
            Assert(CanBeListedWithAuction(input.Symbol, input.TokenId),
                "This NFT cannot be listed with auction for now.");
            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, 1);
            MaybeReceiveRemainDeposit();

            Assert(GetTokenWhiteList(input.Symbol).Value.Contains(input.PurchaseSymbol),
                $"{input.PurchaseSymbol} is not in token white list.");
            Assert(
                State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput {Symbol = input.PurchaseSymbol}).Symbol !=
                null, $"Token {input.PurchaseSymbol} not support purchase for auction.");

            var duration = AdjustListDuration(input.Duration);

            var dutchAuctionInfo = new DutchAuctionInfo
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = duration,
                PurchaseSymbol = input.PurchaseSymbol,
                StartingPrice = input.StartingPrice,
                EndingPrice = input.EndingPrice,
                Owner = Context.Sender
            };
            State.DutchAuctionInfoMap[input.Symbol][input.TokenId] = dutchAuctionInfo;

            var whiteListAddressPriceList = input.WhiteListAddressPriceList;
            if (whiteListAddressPriceList != null)
            {
                State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId] = whiteListAddressPriceList;
            }

            State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] = new ListedNFTInfoList
            {
                Value =
                {
                    new ListedNFTInfo
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        Duration = duration,
                        ListType = ListType.DutchAuction,
                        Owner = Context.Sender,
                        Price = new Price
                        {
                            Symbol = input.PurchaseSymbol,
                            Amount = input.StartingPrice
                        },
                        Quantity = 1
                    }
                }
            };

            ClearBids(input.Symbol, input.TokenId);
            State.EnglishAuctionInfoMap[input.Symbol].Remove(input.TokenId);

            Context.Fire(new ListedNFTInfoChanged
            {
                ListType = ListType.DutchAuction,
                Owner = dutchAuctionInfo.Owner,
                Price = new Price
                {
                    Symbol = input.PurchaseSymbol,
                    Amount = input.StartingPrice
                },
                Quantity = 1,
                Symbol = dutchAuctionInfo.Symbol,
                TokenId = dutchAuctionInfo.TokenId,
                Duration = dutchAuctionInfo.Duration
            });

            return new Empty();
        }

        public override Empty Delist(DelistInput input)
        {
            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, input.Quantity);
            var listedNftInfoList = State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender];
            if (listedNftInfoList == null || listedNftInfoList.Value.All(i => i.ListType == ListType.NotListed))
            {
                throw new AssertionException("Listed NFT Info not exists. (Or already delisted.)");
            }

            Assert(input.Price != null, "Need to specific list record.");
            var listedNftInfo = listedNftInfoList.Value.FirstOrDefault(i =>
                i.Price.Amount == input.Price.Amount && i.Price.Symbol == input.Price.Symbol &&
                i.Owner == Context.Sender);
            if (listedNftInfo == null)
            {
                throw new AssertionException("Listed NFT Info not exists. (Or already delisted.)");
            }

            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                requestInfo.ListTime = null;
                State.RequestInfoMap[input.Symbol][input.TokenId] = requestInfo;
            }

            switch (listedNftInfo.ListType)
            {
                case ListType.FixedPrice when input.Quantity >= listedNftInfo.Quantity:
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId].Remove(Context.Sender);
                    break;
                case ListType.FixedPrice:
                    listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(input.Quantity);
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfoList;
                    break;
                case ListType.EnglishAuction:
                    break;
                case ListType.DutchAuction:
                    break;
            }

            Context.Fire(new ListedNFTInfoChanged
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Owner = Context.Sender,
                ListType = ListType.NotListed,
                Quantity = input.Quantity
            });

            return new Empty();
        }

        /// <summary>
        /// Sender is the seller.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="AssertionException"></exception>
        public override Empty Deal(DealInput input)
        {
            Assert(input.Symbol != null, "Incorrect symbol.");
            Assert(input.TokenId != 0, "Incorrect token id.");
            Assert(input.OfferFrom != null, "Incorrect offer maker.");
            if (input.Price?.Symbol == null)
            {
                throw new AssertionException("Incorrect price.");
            }

            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, input.Quantity);

            var balance = State.NFTContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Owner = Context.Sender
            });
            Assert(balance.Balance >= input.Quantity, "Insufficient balance.");

            var offer = State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom]?.Value
                .FirstOrDefault(o =>
                    o.From == input.OfferFrom && o.Price.Symbol == input.Price.Symbol &&
                    o.Price.Amount == input.Price.Amount);
            var bid = State.BidMap[input.Symbol][input.TokenId][input.OfferFrom];
            Price price;
            long totalAmount;
            if (offer == null)
            {
                // Check bid.

                if (bid == null || bid.From != input.OfferFrom ||
                    bid.Price.Amount != input.Price.Amount || bid.Price.Symbol != input.Price.Symbol)
                {
                    throw new AssertionException("Neither related offer nor bid are found.");
                }

                price = bid.Price;

                Assert(price.TokenId == 0, "Do not support use NFT to purchase auction.");

                var auctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];

                // The earnest money will be transferred to the seller whatever.
                // Buyer need to pay the remain balance (via PerformDeal).
                totalAmount = price.Amount.Sub(auctionInfo.EarnestMoney);

                State.TokenContract.Transfer.VirtualSend(CalculateTokenHash(input.Symbol, input.TokenId),
                    new TransferInput
                    {
                        To = Context.Sender,
                        Symbol = price.Symbol,
                        Amount = auctionInfo.EarnestMoney
                    });

                if (!CheckAllowanceAndBalanceIsEnough(bid.From, price.Symbol, totalAmount))
                {
                    State.BidMap[input.Symbol][input.TokenId].Remove(bid.From);
                    Context.Fire(new BidCanceled
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        BidFrom = input.OfferFrom,
                        BidTo = Context.Sender
                    });
                    return new Empty();
                }

                auctionInfo.DealPrice = input.Price.Amount;
                auctionInfo.DealTo = input.OfferFrom;
                State.EnglishAuctionInfoMap[input.Symbol][input.TokenId] = auctionInfo;

                ClearBids(input.Symbol, input.TokenId);
            }
            else
            {
                Assert(offer.Quantity >= input.Quantity, "Offer quantity exceeded.");
                offer.Quantity = offer.Quantity.Sub(input.Quantity);
                if (offer.Quantity == 0)
                {
                    State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom].Value.Remove(offer);
                }

                price = offer.Price;
                totalAmount = price.Amount.Mul(input.Quantity);
            }

            PerformDeal(new PerformDealInput
            {
                NFTFrom = Context.Sender,
                NFTTo = offer?.From ?? bid.From,
                NFTSymbol = input.Symbol,
                NFTTokenId = input.TokenId,
                NFTQuantity = input.Quantity,
                PurchaseSymbol = price.Symbol,
                PurchaseAmount = totalAmount,
                PurchaseTokenId = price.TokenId
            });
            return new Empty();
        }
    }
}