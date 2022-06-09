using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GetBalanceInput = AElf.Contracts.NFT.GetBalanceInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty ListWithFixedPrice(ListWithFixedPriceInput input)
        {
            AssertContractInitialized();
            Assert(input.Price.Amount > 0, "Incorrect listing price.");
            Assert(input.Quantity > 0, "Incorrect quantity.");
            var duration = AdjustListDuration(input.Duration);
            var whitelists = input.Whitelists;
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            var projectId = CalculateProjectId(input.Symbol, input.TokenId,Context.Sender);
            var whitelistId = new Hash();
            if (input.IsWhitelistAvailable)
            {
                var whitelistManager = GetWhitelistManager();
                if (requestInfo != null)
                {
                    bool isWhiteListDueTimePassed;
                    if (requestInfo.ListTime == null) // Never listed before or delisted before.
                    {
                        isWhiteListDueTimePassed = requestInfo.WhiteListDueTime > Context.CurrentBlockTime;
                    }
                    else
                    {
                        isWhiteListDueTimePassed = requestInfo.ListTime.AddHours(requestInfo.WhiteListHours) >
                                                   Context.CurrentBlockTime;
                    }

                    if (isWhiteListDueTimePassed)
                    {
                        // White list hours not passed -> will refresh list time and white list price.
                        ListRequestedNFT(input, requestInfo, whitelists);
                        duration.StartTime = Context.CurrentBlockTime;
                    }
                    else
                    {
                        MaybeReceiveRemainDeposit(requestInfo);
                    }
                }
                else
                {
                    var extraInfoList = ConvertToExtraInfo(whitelists);
                    //Listed for the first time, create whitelist.
                    if (State.WhitelistIdMap[input.Symbol][input.TokenId][Context.Sender] == null)
                    {
                        State.WhitelistContract.CreateWhitelist.Send(new CreateWhitelistInput()
                        {
                            ProjectId = projectId,
                            StrategyType = StrategyType.Price,
                            Creator = Context.Self,
                            ExtraInfoList = extraInfoList,
                            IsCloneable = true,
                            Remark = $"{input.Symbol}{input.TokenId}"
                        });
                        whitelistId =
                            Context.GenerateId(State.WhitelistContract.Value,
                                ByteArrayHelper.ConcatArrays(Context.Self.ToByteArray(), projectId.ToByteArray()));
                        State.WhitelistIdMap[input.Symbol][input.TokenId][Context.Sender] = whitelistId;
                    }
                    else
                    {
                        //Add address list to the existing whitelist.
                        whitelistId = State.WhitelistIdMap[input.Symbol][input.TokenId][Context.Sender];
                        var extraInfoIdList = whitelists?.Whitelists.GroupBy(p => p.PriceTag)
                            .ToDictionary(e=>e.Key, e =>e.ToList())
                            .Select(extra =>
                            {
                                //Whether price tag already exists.
                                var ifExist = State.WhitelistContract.GetTagInfoFromWhitelist.Call(
                                    new GetTagInfoFromWhitelistInput()
                                    {
                                        ProjectId = projectId,
                                        WhitelistId = whitelistId,
                                        TagInfo = new TagInfo()
                                            {TagName = extra.Key.TagName, Info = extra.Key.Price.ToByteString()}
                                    }).Value;
                                if (!ifExist)
                                {
                                    //Doesn't exist,add tag info.
                                    State.WhitelistContract.AddExtraInfo.Send(new AddExtraInfoInput()
                                    {
                                        ProjectId = projectId,
                                        WhitelistId = whitelistId,
                                        TagInfo = new TagInfo()
                                            {TagName = extra.Key.TagName, Info = extra.Key.Price.ToByteString()}
                                    });
                                }
                                var tagId =
                                    HashHelper.ComputeFrom(
                                        $"{whitelistId}{projectId}{extra.Key.TagName}");
                                var toAddExtraInfoIdList = new ExtraInfoIdList();
                                foreach (var whitelistInfo in extra.Value.Where(whitelistInfo => whitelistInfo.AddressList.Value.Any()))
                                {
                                    toAddExtraInfoIdList.Value.Add(new ExtraInfoId()
                                    {
                                        AddressList = new Whitelist.AddressList
                                        {
                                            Value = {whitelistInfo.AddressList.Value}
                                        },
                                        Id = tagId
                                    });
                                }
                                return toAddExtraInfoIdList;
                            }).ToList();
                        if (extraInfoList != null && extraInfoIdList != null && extraInfoIdList.Count != 0)
                        {
                            var toAdd = new ExtraInfoIdList();
                            foreach (var extra in extraInfoIdList)
                            {
                                toAdd.Value.Add(extra.Value);
                            }
                            State.WhitelistContract.AddAddressInfoListToWhitelist.Send(
                                new AddAddressInfoListToWhitelistInput()
                                {
                                    WhitelistId = whitelistId,
                                    ExtraInfoIdList = toAdd
                                    
                                });
                        }
                    }
                }
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

            bool isMergedToPreviousListedInfo;
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
                isMergedToPreviousListedInfo = false;
                Context.Fire(new ListedNFTAdded
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Duration = duration,
                    Owner = Context.Sender,
                    Price = input.Price,
                    Quantity = input.Quantity,
                    WhitelistId = whitelistId
                });
            }
            else
            {
                listedNftInfo.Quantity = listedNftInfo.Quantity.Add(input.Quantity);
                var previousDuration = listedNftInfo.Duration.Clone();
                listedNftInfo.Duration = duration;
                isMergedToPreviousListedInfo = true;
                Context.Fire(new ListedNFTChanged
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Duration = duration,
                    Owner = Context.Sender,
                    Price = input.Price,
                    Quantity = listedNftInfo.Quantity,
                    PreviousDuration = previousDuration
                });
            }

            State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfoList;

            var totalQuantity = listedNftInfoList.Value.Where(i => i.Owner == Context.Sender).Sum(i => i.Quantity);
            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, totalQuantity);

            ClearBids(input.Symbol, input.TokenId);
            State.EnglishAuctionInfoMap[input.Symbol].Remove(input.TokenId);
            State.DutchAuctionInfoMap[input.Symbol].Remove(input.TokenId);

            Context.Fire(new FixedPriceNFTListed
            {
                Owner = listedNftInfo.Owner,
                Price = listedNftInfo.Price,
                Quantity = listedNftInfo.Quantity,
                Symbol = listedNftInfo.Symbol,
                TokenId = listedNftInfo.TokenId,
                Duration = listedNftInfo.Duration,
                IsMergedToPreviousListedInfo = isMergedToPreviousListedInfo,
                WhitelistId = whitelistId
            });

            return new Empty();
        }

        public override Empty ListWithEnglishAuction(ListWithEnglishAuctionInput input)
        {
            AssertContractInitialized();
            Assert(input.StartingPrice > 0, "Incorrect listing price.");
            Assert(input.EarnestMoney <= input.StartingPrice, "Earnest money too high.");
            if (CanBeListedWithAuction(input.Symbol, input.TokenId, out var requestInfo))
            {
                MaybeReceiveRemainDeposit(requestInfo);
            }
            else
            {
                throw new AssertionException("This NFT cannot be listed with auction for now.");
            }

            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, 1);

            Assert(GetTokenWhiteList(input.Symbol).Value.Contains(input.PurchaseSymbol),
                $"{input.PurchaseSymbol} is not in token white list.");
            Assert(
                string.IsNullOrEmpty(State.NFTContract.GetNFTProtocolInfo
                    .Call(new StringValue {Value = input.PurchaseSymbol}).Symbol),
                $"Token {input.PurchaseSymbol} not support purchase for auction.");

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

            Context.Fire(new EnglishAuctionNFTListed
            {
                Owner = englishAuctionInfo.Owner,
                Symbol = englishAuctionInfo.Symbol,
                PurchaseSymbol = englishAuctionInfo.PurchaseSymbol,
                StartingPrice = englishAuctionInfo.StartingPrice,
                TokenId = englishAuctionInfo.TokenId,
                Duration = englishAuctionInfo.Duration,
                EarnestMoney = englishAuctionInfo.EarnestMoney
            });

            Context.Fire(new ListedNFTAdded
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = englishAuctionInfo.Duration,
                Owner = englishAuctionInfo.Owner,
                Price = new Price
                {
                    Symbol = englishAuctionInfo.PurchaseSymbol,
                    Amount = englishAuctionInfo.StartingPrice
                },
                Quantity = 1
            });

            return new Empty();
        }

        public override Empty ListWithDutchAuction(ListWithDutchAuctionInput input)
        {
            AssertContractInitialized();
            Assert(input.StartingPrice > 0 && input.EndingPrice > 0 && input.StartingPrice > input.EndingPrice,
                "Incorrect listing price.");
            if (CanBeListedWithAuction(input.Symbol, input.TokenId, out var requestInfo))
            {
                MaybeReceiveRemainDeposit(requestInfo);
            }
            else
            {
                throw new AssertionException("This NFT cannot be listed with auction for now.");
            }

            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, 1);

            Assert(GetTokenWhiteList(input.Symbol).Value.Contains(input.PurchaseSymbol),
                $"{input.PurchaseSymbol} is not in token white list.");
            Assert(
                string.IsNullOrEmpty(State.NFTContract.GetNFTProtocolInfo
                    .Call(new StringValue {Value = input.PurchaseSymbol}).Symbol),
                $"Token {input.PurchaseSymbol} not support purchase for auction.");

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

            Context.Fire(new DutchAuctionNFTListed
            {
                Owner = dutchAuctionInfo.Owner,
                PurchaseSymbol = dutchAuctionInfo.PurchaseSymbol,
                StartingPrice = dutchAuctionInfo.StartingPrice,
                EndingPrice = dutchAuctionInfo.EndingPrice,
                Symbol = dutchAuctionInfo.Symbol,
                TokenId = dutchAuctionInfo.TokenId,
                Duration = dutchAuctionInfo.Duration
            });

            Context.Fire(new ListedNFTAdded
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = dutchAuctionInfo.Duration,
                Owner = dutchAuctionInfo.Owner,
                Price = new Price
                {
                    Symbol = dutchAuctionInfo.PurchaseSymbol,
                    Amount = dutchAuctionInfo.StartingPrice
                },
                Quantity = 1
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
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender].Value.Remove(listedNftInfo);
                    Context.Fire(new ListedNFTRemoved
                    {
                        Symbol = listedNftInfo.Symbol,
                        TokenId = listedNftInfo.TokenId,
                        Duration = listedNftInfo.Duration,
                        Owner = listedNftInfo.Owner
                    });
                    break;
                case ListType.FixedPrice:
                    listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(input.Quantity);
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfoList;
                    Context.Fire(new ListedNFTChanged
                    {
                        Symbol = listedNftInfo.Symbol,
                        TokenId = listedNftInfo.TokenId,
                        Duration = listedNftInfo.Duration,
                        Owner = listedNftInfo.Owner,
                        Price = listedNftInfo.Price,
                        Quantity = listedNftInfo.Quantity
                    });
                    break;
                case ListType.EnglishAuction:
                    var englishAuctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];
                    var bidAddressList = State.BidAddressListMap[input.Symbol][input.TokenId];
                    if (bidAddressList != null && bidAddressList.Value.Any())
                    {
                        // Charge service fee if anyone placed a bid.
                        ChargeSenderServiceFee(englishAuctionInfo.PurchaseSymbol, englishAuctionInfo.StartingPrice);
                    }
                    ClearBids(englishAuctionInfo.Symbol, englishAuctionInfo.TokenId);
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender].Value.Remove(listedNftInfo);
                    State.EnglishAuctionInfoMap[input.Symbol].Remove(input.TokenId);
                    Context.Fire(new ListedNFTRemoved
                    {
                        Symbol = listedNftInfo.Symbol,
                        TokenId = listedNftInfo.TokenId,
                        Duration = listedNftInfo.Duration,
                        Owner = listedNftInfo.Owner
                    });
                    break;
                case ListType.DutchAuction:
                    var dutchAuctionInfo = State.DutchAuctionInfoMap[input.Symbol][input.TokenId];
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender].Value.Remove(listedNftInfo);
                    State.DutchAuctionInfoMap[input.Symbol].Remove(input.TokenId);
                    ChargeSenderServiceFee(dutchAuctionInfo.PurchaseSymbol, dutchAuctionInfo.StartingPrice);
                    Context.Fire(new ListedNFTRemoved
                    {
                        Symbol = listedNftInfo.Symbol,
                        TokenId = listedNftInfo.TokenId,
                        Duration = listedNftInfo.Duration,
                        Owner = listedNftInfo.Owner
                    });
                    break;
            }

            Context.Fire(new NFTDelisted
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Owner = Context.Sender,
                Quantity = input.Quantity
            });

            return new Empty();
        }

        private void ChargeSenderServiceFee(string symbol, long baseAmount)
        {
            var amount = baseAmount.Mul(State.ServiceFeeRate.Value).Div(FeeDenominator);
            if (amount > 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    Symbol = symbol,
                    Amount = amount,
                    From = Context.Sender,
                    To = State.ServiceFeeReceiver.Value ?? State.Admin.Value
                });
            }
        }

        /// <summary>
        /// Sender is the seller.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="AssertionException"></exception>
        public override Empty Deal(DealInput input)
        {
            AssertContractInitialized();

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
            Assert(balance.Balance >= input.Quantity, "Insufficient NFT balance.");

            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                Assert(Context.CurrentBlockTime > requestInfo.WhiteListDueTime, "Due time not passed.");
            }

            var offer = State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom]?.Value
                .FirstOrDefault(o =>
                    o.From == input.OfferFrom && o.Price.Symbol == input.Price.Symbol &&
                    o.Price.Amount == input.Price.Amount && o.ExpireTime >= Context.CurrentBlockTime);
            var bid = State.BidMap[input.Symbol][input.TokenId][input.OfferFrom];
            Price price;
            long totalAmount;
            if (offer == null)
            {
                // Check bid.

                if (bid == null || bid.From != input.OfferFrom ||
                    bid.Price.Amount != input.Price.Amount || bid.Price.Symbol != input.Price.Symbol ||
                    bid.ExpireTime < Context.CurrentBlockTime)
                {
                    throw new AssertionException("Neither related offer nor bid are found.");
                }

                price = bid.Price;

                Assert(price.TokenId == 0, "Do not support use NFT to purchase auction.");

                var auctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];

                totalAmount = price.Amount;

                // Transfer earnest money back to the bidder at first.
                if (auctionInfo.EarnestMoney > 0)
                {
                    State.TokenContract.Transfer.VirtualSend(CalculateTokenHash(input.Symbol, input.TokenId),
                        new TransferInput
                        {
                            To = bid.From,
                            Symbol = price.Symbol,
                            Amount = auctionInfo.EarnestMoney
                        });
                }

                if (!CheckAllowanceAndBalanceIsEnough(bid.From, price.Symbol, totalAmount.Sub(auctionInfo.EarnestMoney)))
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

                ClearBids(input.Symbol, input.TokenId, input.OfferFrom);
            }
            else
            {
                Assert(offer.Quantity >= input.Quantity, "Deal quantity exceeded.");
                offer.Quantity = offer.Quantity.Sub(input.Quantity);
                if (offer.Quantity == 0)
                {
                    State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom].Value.Remove(offer);
                    Context.Fire(new OfferRemoved
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        OfferFrom = input.OfferFrom,
                        OfferTo = Context.Sender,
                        ExpireTime = offer.ExpireTime
                    });
                }

                price = offer.Price;
                totalAmount = price.Amount.Mul(input.Quantity);
                Context.Fire(new OfferChanged
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    OfferFrom = input.OfferFrom,
                    OfferTo = Context.Sender,
                    Quantity = input.Quantity,
                    Price = new Price
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        Amount = totalAmount
                    },
                    ExpireTime = offer.ExpireTime
                });
            }

            var listedNftInfoList = State.ListedNFTInfoListMap[input.Symbol][input.TokenId][Context.Sender];
            if (listedNftInfoList != null && listedNftInfoList.Value.Any())
            {
                var firstListedNftInfo = listedNftInfoList.Value.First();
                if (firstListedNftInfo.ListType != ListType.EnglishAuction && firstListedNftInfo.ListType != ListType.DutchAuction)
                {
                    // Listed with fixed price.

                    var nftBalance = State.NFTContract.GetBalance.Call(new GetBalanceInput
                    {
                        Symbol = input.Symbol,
                        Owner = Context.Sender,
                        TokenId = input.TokenId
                    }).Balance;
                    var listedQuantity = listedNftInfoList.Value.Where(i => i.Owner == Context.Sender).Sum(i => i.Quantity);
                    Assert(nftBalance >= listedQuantity.Add(input.Quantity),
                        $"Need to delist at least {listedQuantity.Add(input.Quantity).Sub(nftBalance)} NFT(s) before deal.");
                }
                else
                {
                    State.ListedNFTInfoListMap[input.Symbol][input.TokenId].Remove(Context.Sender);
                    Context.Fire(new ListedNFTRemoved
                    {
                        Symbol = firstListedNftInfo.Symbol,
                        TokenId = firstListedNftInfo.TokenId,
                        Duration = firstListedNftInfo.Duration,
                        Owner = firstListedNftInfo.Owner
                    });
                }
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