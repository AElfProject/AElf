using System;
using System.Linq;
using AElf.Contracts.NFT;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GetAllowanceInput = AElf.Contracts.MultiToken.GetAllowanceInput;
using GetBalanceInput = AElf.Contracts.MultiToken.GetBalanceInput;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        /// <summary>
        /// There are 2 types of making offer.
        /// 1. Aiming a owner.
        /// 2. Only aiming nft. Owner will be the nft protocol creator.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty MakeOffer(MakeOfferInput input)
        {
            AssertContractInitialized();

            Assert(Context.Sender != input.OfferTo, "Origin owner cannot be sender himself.");

            var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });

            if (nftInfo.Quantity != 0 && input.OfferTo == null)
            {
                input.OfferTo = nftInfo.Creator;
            }

            var protocolInfo = State.NFTContract.GetNFTProtocolInfo.Call(new StringValue {Value = input.Symbol});

            if (nftInfo.Quantity == 0 && !protocolInfo.IsTokenIdReuse && input.Quantity == 1)
            {
                // NFT not minted.
                PerformRequestNewItem(input.Symbol, input.TokenId, input.Price, input.ExpireTime);
                return new Empty();
            }

            Assert(nftInfo.Quantity > 0, "NFT does not exist.");

            var listedNftInfoList = State.ListedNFTInfoListMap[input.Symbol][input.TokenId][input.OfferTo];

            if (listedNftInfoList == null || listedNftInfoList.Value.All(i => i.ListType == ListType.NotListed))
            {
                // NFT not listed by the owner.
                PerformMakeOffer(input);
                return new Empty();
            }

            var validListedNftInfoList = listedNftInfoList.Value.Where(i =>
                (i.Price.Symbol == input.Price.Symbol && i.Price.Amount <= input.Price.Amount ||
                 i.ListType != ListType.FixedPrice) &&
                !IsListedNftTimedOut(i)).ToList();
            ListedNFTInfo listedNftInfo;
            if (validListedNftInfoList.Any())
            {
                listedNftInfo = validListedNftInfoList.First();

                if (validListedNftInfoList.Count > 1)
                {
                    var totalQuantity = validListedNftInfoList.Sum(i => i.Quantity);
                    listedNftInfo.Quantity = totalQuantity;
                }
            }
            else
            {
                listedNftInfo = listedNftInfoList.Value.First();
            }
            
            var whitelistId = State.WhitelistIdMap[input.Symbol][input.TokenId][input.OfferTo];
            var tagInfo = new TagInfo();
            if (listedNftInfo == null || listedNftInfo.ListType == ListType.NotListed)
            {
                if (whitelistId == null)
                {
                    PerformMakeOffer(input);
                    return new Empty();
                }
                //Whether buyer have their own price. 
                tagInfo = State.WhitelistContract.GetExtraInfoByAddress.Call(new GetExtraInfoByAddressInput()
                {
                    Address = Context.Sender,
                    WhitelistId = whitelistId
                });
                var price = DeserializedInfo(tagInfo);
                if (price.Amount <= input.Price.Amount && price.Symbol == input.Price.Symbol)
                {
                    listedNftInfo = new ListedNFTInfo
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        Price = price,
                        ListType = ListType.FixedPrice,
                        Quantity = 1,
                        Owner = listedNftInfoList.Value.First().Owner,
                        Duration = listedNftInfoList.Value.First().Duration
                    };
                }
                else
                {
                    PerformMakeOffer(input);
                    return new Empty();
                }
            }

            var quantity = input.Quantity;
            if (quantity > listedNftInfo.Quantity)
            {
                var makerOfferInput = input.Clone();
                makerOfferInput.Quantity = quantity.Sub(listedNftInfo.Quantity);
                PerformMakeOffer(makerOfferInput);
                input.Quantity = listedNftInfo.Quantity;
            }

            if (IsListedNftTimedOut(listedNftInfo))
            {
                PerformMakeOffer(input);
                return new Empty();
            }

            switch (listedNftInfo.ListType)
            {
                case ListType.FixedPrice when whitelistId != null && tagInfo != null:
                    if (TryDealWithFixedPrice(input, listedNftInfo, out var actualQuantity))
                    {
                        MaybeRemoveRequest(input.Symbol, input.TokenId);
                        listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(actualQuantity);
                        if (listedNftInfo.Quantity == 0 && listedNftInfoList.Value.Contains(listedNftInfo))
                        {
                            listedNftInfoList.Value.Remove(listedNftInfo);
                            Context.Fire(new ListedNFTRemoved
                            {
                                Symbol = listedNftInfo.Symbol,
                                TokenId = listedNftInfo.TokenId,
                                Duration = listedNftInfo.Duration,
                                Owner = listedNftInfo.Owner
                            });
                        }
                        else
                        {
                            Context.Fire(new ListedNFTChanged
                            {
                                Symbol = listedNftInfo.Symbol,
                                TokenId = listedNftInfo.TokenId,
                                Duration = listedNftInfo.Duration,
                                Owner = listedNftInfo.Owner,
                                PreviousDuration = listedNftInfo.Duration,
                                Quantity = listedNftInfo.Quantity,
                                Price = listedNftInfo.Price
                            });
                        }
                    }

                    break;
                case ListType.FixedPrice when input.Price.Symbol == listedNftInfo.Price.Symbol &&
                                              input.Price.Amount >= listedNftInfo.Price.Amount:
                    input.Price.Amount = Math.Min(input.Price.Amount, listedNftInfo.Price.Amount);
                    input.Quantity = Math.Min(input.Quantity, listedNftInfo.Quantity);
                    if (TryDealWithFixedPrice(input, listedNftInfo, out var dealQuantity))
                    {
                        listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(dealQuantity);
                        if (listedNftInfo.Quantity == 0)
                        {
                            listedNftInfoList.Value.Remove(listedNftInfo);
                            Context.Fire(new ListedNFTRemoved
                            {
                                Symbol = listedNftInfo.Symbol,
                                TokenId = listedNftInfo.TokenId,
                                Duration = listedNftInfo.Duration,
                                Owner = listedNftInfo.Owner
                            });
                        }
                        else
                        {
                            Context.Fire(new ListedNFTChanged
                            {
                                Symbol = listedNftInfo.Symbol,
                                TokenId = listedNftInfo.TokenId,
                                Duration = listedNftInfo.Duration,
                                Owner = listedNftInfo.Owner,
                                PreviousDuration = listedNftInfo.Duration,
                                Quantity = listedNftInfo.Quantity,
                                Price = listedNftInfo.Price
                            });
                        }
                    }

                    break;

                case ListType.EnglishAuction:
                    TryPlaceBidForEnglishAuction(input);
                    break;
                case ListType.DutchAuction:
                    if (PerformMakeOfferToDutchAuction(input))
                    {
                        listedNftInfoList.Value.Remove(listedNftInfo);
                    }

                    break;
                default:
                    PerformMakeOffer(input);
                    break;
            }

            State.ListedNFTInfoListMap[input.Symbol][input.TokenId][input.OfferTo] = listedNftInfoList;

            return new Empty();
        }

        public override Empty CancelOffer(CancelOfferInput input)
        {
            AssertContractInitialized();

            OfferList offerList;
            var newOfferList = new OfferList();
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];

            // Admin can remove expired offer.
            if (input.OfferFrom != null && input.OfferFrom != Context.Sender)
            {
                AssertSenderIsAdmin();

                offerList = State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom];

                if (offerList != null)
                {
                    foreach (var offer in offerList.Value)
                    {
                        if (offer.ExpireTime >= Context.CurrentBlockTime)
                        {
                            newOfferList.Value.Add(offer);
                        }
                        else
                        {
                            Context.Fire(new OfferRemoved
                            {
                                Symbol = input.Symbol,
                                TokenId = input.TokenId,
                                OfferFrom = offer.From,
                                OfferTo = offer.To,
                                ExpireTime = offer.ExpireTime
                            });
                        }
                    }

                    State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom] = newOfferList;
                }

                if (requestInfo != null && !requestInfo.IsConfirmed &&
                    requestInfo.ExpireTime > Context.CurrentBlockTime)
                {
                    MaybeRemoveRequest(input.Symbol, input.TokenId);
                    var protocolVirtualAddressFrom = CalculateTokenHash(input.Symbol);
                    var protocolVirtualAddress =
                        Context.ConvertVirtualAddressToContractAddress(protocolVirtualAddressFrom);
                    var balanceOfNftProtocolVirtualAddress = State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Symbol = requestInfo.Price.Symbol,
                        Owner = protocolVirtualAddress
                    }).Balance;

                    if (balanceOfNftProtocolVirtualAddress > 0)
                    {
                        State.TokenContract.Transfer.VirtualSend(protocolVirtualAddressFrom, new TransferInput
                        {
                            To = requestInfo.Requester,
                            Symbol = requestInfo.Price.Symbol,
                            Amount = balanceOfNftProtocolVirtualAddress
                        });
                    }

                    Context.Fire(new NFTRequestCancelled
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        Requester = Context.Sender
                    });
                }

                var bid = State.BidMap[input.Symbol][input.TokenId][input.OfferFrom];

                if (bid != null)
                {
                    if (bid.ExpireTime < Context.CurrentBlockTime)
                    {
                        State.BidMap[input.Symbol][input.TokenId].Remove(input.OfferFrom);
                        var auctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];
                        if (auctionInfo != null && auctionInfo.EarnestMoney > 0)
                        {
                            State.TokenContract.Transfer.VirtualSend(CalculateTokenHash(input.Symbol, input.TokenId),
                                new TransferInput
                                {
                                    To = bid.From,
                                    Symbol = auctionInfo.PurchaseSymbol,
                                    Amount = auctionInfo.EarnestMoney
                                });
                        }

                        var bidAddressList = State.BidAddressListMap[input.Symbol][input.TokenId];
                        if (bidAddressList != null && bidAddressList.Value.Contains(Context.Sender))
                        {
                            State.BidAddressListMap[input.Symbol][input.TokenId].Value.Remove(Context.Sender);
                        }

                        Context.Fire(new BidCanceled
                        {
                            Symbol = input.Symbol,
                            TokenId = input.TokenId,
                            BidFrom = bid.From,
                            BidTo = bid.To
                        });
                    }
                }

                return new Empty();
            }

            offerList = State.OfferListMap[input.Symbol][input.TokenId][Context.Sender];

            // Check Request Map first.
            if (requestInfo != null)
            {
                PerformCancelRequest(input, requestInfo);
                // Only one request for each token id.
                Context.Fire(new OfferRemoved
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    OfferFrom = Context.Sender,
                    OfferTo = offerList.Value.FirstOrDefault()?.To,
                    ExpireTime = offerList.Value.FirstOrDefault()?.ExpireTime
                });
                State.OfferListMap[input.Symbol][input.TokenId].Remove(Context.Sender);
                return new Empty();
            }

            var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });
            if (nftInfo.Creator == null)
            {
                // This nft does not exist.
                State.OfferListMap[input.Symbol][input.TokenId].Remove(Context.Sender);
            }

            if (input.IsCancelBid)
            {
                var bid = State.BidMap[input.Symbol][input.TokenId][Context.Sender];
                if (bid != null)
                {
                    var auctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];
                    var finishTime = auctionInfo.Duration.StartTime.AddHours(auctionInfo.Duration.DurationHours);
                    if (auctionInfo.DealTo != null || Context.CurrentBlockTime >= finishTime ||
                        Context.CurrentBlockTime >= bid.ExpireTime)
                    {
                        if (auctionInfo.EarnestMoney > 0)
                        {
                            State.TokenContract.Transfer.VirtualSend(CalculateTokenHash(input.Symbol, input.TokenId),
                                new TransferInput
                                {
                                    To = Context.Sender,
                                    Symbol = auctionInfo.PurchaseSymbol,
                                    Amount = auctionInfo.EarnestMoney
                                });
                        }
                    }

                    State.BidMap[input.Symbol][input.TokenId].Remove(Context.Sender);

                    var bidAddressList = State.BidAddressListMap[input.Symbol][input.TokenId];
                    if (bidAddressList != null && bidAddressList.Value.Contains(Context.Sender))
                    {
                        State.BidAddressListMap[input.Symbol][input.TokenId].Value.Remove(Context.Sender);
                    }

                    Context.Fire(new BidCanceled
                    {
                        Symbol = input.Symbol,
                        TokenId = input.TokenId,
                        BidFrom = Context.Sender,
                        BidTo = bid.To
                    });
                }
            }

            if (input.IndexList != null && input.IndexList.Value.Any())
            {
                for (var i = 0; i < offerList.Value.Count; i++)
                {
                    if (!input.IndexList.Value.Contains(i))
                    {
                        newOfferList.Value.Add(offerList.Value[i]);
                    }
                    else
                    {
                        Context.Fire(new OfferRemoved
                        {
                            Symbol = input.Symbol,
                            TokenId = input.TokenId,
                            OfferFrom = Context.Sender,
                            OfferTo = offerList.Value[i].To,
                            ExpireTime = offerList.Value[i].ExpireTime
                        });
                    }
                }

                Context.Fire(new OfferCanceled
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    OfferFrom = Context.Sender,
                    IndexList = input.IndexList
                });
            }
            else
            {
                newOfferList.Value.Add(offerList.Value);
            }

            State.OfferListMap[input.Symbol][input.TokenId][Context.Sender] = newOfferList;

            return new Empty();
        }

        private void PerformCancelRequest(CancelOfferInput input, RequestInfo requestInfo)
        {
            Assert(requestInfo.Requester == Context.Sender, "No permission.");
            var virtualAddress = CalculateNFTVirtuaAddress(input.Symbol, input.TokenId);
            var balanceOfNftVirtualAddress = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = requestInfo.Price.Symbol,
                Owner = virtualAddress
            }).Balance;

            var depositReceiver = requestInfo.Requester;

            if (requestInfo.IsConfirmed)
            {
                if (requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours) < Context.CurrentBlockTime)
                {
                    // Creator missed the deadline.

                    var protocolVirtualAddressFrom = CalculateTokenHash(input.Symbol);
                    var protocolVirtualAddress =
                        Context.ConvertVirtualAddressToContractAddress(protocolVirtualAddressFrom);
                    var balanceOfNftProtocolVirtualAddress = State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Symbol = requestInfo.Price.Symbol,
                        Owner = protocolVirtualAddress
                    }).Balance;
                    var deposit = balanceOfNftVirtualAddress.Mul(FeeDenominator).Div(DefaultDepositConfirmRate)
                        .Sub(balanceOfNftVirtualAddress);
                    if (balanceOfNftProtocolVirtualAddress > 0)
                    {
                        State.TokenContract.Transfer.VirtualSend(protocolVirtualAddressFrom, new TransferInput
                        {
                            To = requestInfo.Requester,
                            Symbol = requestInfo.Price.Symbol,
                            Amount = Math.Min(balanceOfNftProtocolVirtualAddress, deposit)
                        });
                    }
                }
                else
                {
                    depositReceiver = State.NFTContract.GetNFTProtocolInfo.Call(new StringValue {Value = input.Symbol})
                        .Creator;
                }
            }

            var virtualAddressFrom = CalculateTokenHash(input.Symbol, input.TokenId);

            if (balanceOfNftVirtualAddress > 0)
            {
                State.TokenContract.Transfer.VirtualSend(virtualAddressFrom, new TransferInput
                {
                    To = depositReceiver,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = balanceOfNftVirtualAddress
                });
            }

            MaybeRemoveRequest(input.Symbol, input.TokenId);

            Context.Fire(new NFTRequestCancelled
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Requester = Context.Sender
            });
        }

        /// <summary>
        /// Sender is buyer.
        /// </summary>
        private bool TryDealWithFixedPrice(MakeOfferInput input, ListedNFTInfo listedNftInfo ,out long actualQuantity)
        {
            var whitelistId = State.WhitelistIdMap[input.Symbol][input.TokenId][input.OfferTo];
            TagInfo whitelistPrice = null; 
            if (whitelistId != null)
            {
                whitelistPrice = State.WhitelistContract.GetExtraInfoByAddress.Call(new GetExtraInfoByAddressInput()
                {
                    Address = Context.Sender,
                    WhitelistId = whitelistId
                });
            }
            var usePrice = input.Price;
            actualQuantity = Math.Min(input.Quantity, listedNftInfo.Quantity);
            if (Context.CurrentBlockTime < listedNftInfo.Duration.StartTime)
            {
                PerformMakeOffer(input);
                return false;
            }

            if (whitelistPrice != null)
            {
                var price = DeserializedInfo(whitelistPrice);
                // May cause problems, but can be fixed via re-sorting the white list price list.
                Assert(input.Price.Symbol == price.Symbol,
                    $"Need to use token {price.Symbol}, not {input.Price.Symbol}");
                if (input.Price.Amount < price.Amount)
                {
                    PerformMakeOffer(input);
                    return false;
                }

                usePrice = price;
                if (actualQuantity > 1)
                {
                    var makeOfferInput = input.Clone();
                    makeOfferInput.Quantity = actualQuantity.Sub(1);
                    PerformMakeOffer(makeOfferInput);
                }
                // One record in white list price list for one NFT.
                actualQuantity = 1;
                //Get extraInfoId according to the sender.
                var extraInfoId = State.WhitelistContract.GetTagIdByAddress.Call(new GetTagIdByAddressInput()
                {
                    WhitelistId = whitelistId,
                    Address = Context.Sender
                });
                State.WhitelistContract.RemoveAddressInfoListFromWhitelist.Send(new RemoveAddressInfoListFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList()
                    {
                        Value = { new ExtraInfoId
                        {
                            AddressList = new Whitelist.AddressList {Value = {Context.Sender}},
                            Id = extraInfoId
                        } }
                    }
                });
            }
            else if (listedNftInfo.Duration.PublicTime > Context.CurrentBlockTime)
            {
                // Public time not reached and sender is not in white list.
                PerformMakeOffer(input);
                return false;
            }

            var totalAmount = usePrice.Amount.Mul(actualQuantity);
            PerformDeal(new PerformDealInput
            {
                NFTFrom = input.OfferTo,
                NFTTo = Context.Sender,
                NFTSymbol = input.Symbol,
                NFTTokenId = input.TokenId,
                NFTQuantity = actualQuantity,
                PurchaseSymbol = usePrice.Symbol,
                PurchaseAmount = totalAmount,
                PurchaseTokenId = input.Price.TokenId
            });

            return true;
        }

        /// <summary>
        /// Will go to Offer List.
        /// </summary>
        /// <param name="input"></param>
        private void PerformMakeOffer(MakeOfferInput input)
        {
            var offerList = State.OfferListMap[input.Symbol][input.TokenId][Context.Sender] ?? new OfferList();
            var expireTime = input.ExpireTime ?? Context.CurrentBlockTime.AddDays(DefaultExpireDays);
            var maybeSameOffer = offerList.Value.SingleOrDefault(o =>
                o.Price.Symbol == input.Price.Symbol && o.Price.Amount == input.Price.Amount &&
                o.ExpireTime == expireTime && o.To == input.OfferTo && o.From == Context.Sender);
            if (maybeSameOffer == null)
            {
                offerList.Value.Add(new Offer
                {
                    From = Context.Sender,
                    To = input.OfferTo,
                    Price = input.Price,
                    ExpireTime = expireTime,
                    Quantity = input.Quantity
                });
                Context.Fire(new OfferAdded
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    OfferFrom = Context.Sender,
                    OfferTo = input.OfferTo,
                    ExpireTime = expireTime,
                    Price = input.Price,
                    Quantity = input.Quantity
                });
            }
            else
            {
                maybeSameOffer.Quantity = maybeSameOffer.Quantity.Add(input.Quantity);
                Context.Fire(new OfferChanged
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    OfferFrom = Context.Sender,
                    OfferTo = input.OfferTo,
                    Price = input.Price,
                    ExpireTime = expireTime,
                    Quantity = maybeSameOffer.Quantity
                });
            }

            State.OfferListMap[input.Symbol][input.TokenId][Context.Sender] = offerList;

            var addressList = State.OfferAddressListMap[input.Symbol][input.TokenId] ?? new AddressList();

            if (!addressList.Value.Contains(Context.Sender))
            {
                addressList.Value.Add(Context.Sender);
                State.OfferAddressListMap[input.Symbol][input.TokenId] = addressList;
            }

            Context.Fire(new OfferMade
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                OfferFrom = Context.Sender,
                OfferTo = input.OfferTo,
                ExpireTime = expireTime,
                Price = input.Price,
                Quantity = input.Quantity
            });
        }

        private void TryPlaceBidForEnglishAuction(MakeOfferInput input)
        {
            var auctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];
            if (auctionInfo == null)
            {
                throw new AssertionException($"Auction info of {input.Symbol}-{input.TokenId} not found.");
            }

            var duration = auctionInfo.Duration;
            if (Context.CurrentBlockTime < duration.StartTime)
            {
                PerformMakeOffer(input);
                return;
            }

            Assert(Context.CurrentBlockTime <= duration.StartTime.AddHours(duration.DurationHours),
                "Auction already finished.");
            Assert(input.Price.Symbol == auctionInfo.PurchaseSymbol, "Incorrect symbol.");
            Assert(input.Price.TokenId == 0, "Do not support use NFT to purchase auction.");

            if (input.Price.Amount < auctionInfo.StartingPrice)
            {
                PerformMakeOffer(input);
                return;
            }

            var bidList = GetBidList(new GetBidListInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });
            var sortedBitList = new BidList
            {
                Value =
                {
                    bidList.Value.OrderByDescending(o => o.Price.Amount)
                }
            };
            if (sortedBitList.Value.Any() && input.Price.Amount <= sortedBitList.Value.First().Price.Amount)
            {
                PerformMakeOffer(input);
                return;
            }

            var bid = new Bid
            {
                From = Context.Sender,
                To = input.OfferTo,
                Price = new Price
                {
                    Symbol = input.Price.Symbol,
                    Amount = input.Price.Amount
                },
                ExpireTime = input.ExpireTime ?? Context.CurrentBlockTime.AddDays(DefaultExpireDays)
            };

            var bidAddressList = State.BidAddressListMap[input.Symbol][input.TokenId] ?? new AddressList();
            if (!bidAddressList.Value.Contains(Context.Sender))
            {
                bidAddressList.Value.Add(Context.Sender);
                State.BidAddressListMap[input.Symbol][input.TokenId] = bidAddressList;
                // Charge earnest if the Sender is the first time to place a bid.
                ChargeEarnestMoney(input.Symbol, input.TokenId, auctionInfo.PurchaseSymbol, auctionInfo.EarnestMoney);
            }

            State.BidMap[input.Symbol][input.TokenId][Context.Sender] = bid;

            var remainAmount = input.Price.Amount.Sub(auctionInfo.EarnestMoney);
            Assert(
                State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Symbol = auctionInfo.PurchaseSymbol,
                    Owner = Context.Sender
                }).Balance >= remainAmount,
                "Insufficient balance to bid.");
            Assert(
                State.TokenContract.GetAllowance.Call(new GetAllowanceInput
                {
                    Symbol = auctionInfo.PurchaseSymbol,
                    Owner = Context.Sender,
                    Spender = Context.Self
                }).Allowance >= remainAmount,
                "Insufficient allowance to bid.");

            Context.Fire(new BidPlaced
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Price = bid.Price,
                ExpireTime = bid.ExpireTime,
                OfferFrom = bid.From,
                OfferTo = input.OfferTo
            });
        }

        private void ChargeEarnestMoney(string nftSymbol, long nftTokenId, string purchaseSymbol, long earnestMoney)
        {
            if (earnestMoney > 0)
            {
                var virtualAddress = CalculateNFTVirtuaAddress(nftSymbol, nftTokenId);
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = virtualAddress,
                    Symbol = purchaseSymbol,
                    Amount = earnestMoney
                });
            }
        }

        private bool PerformMakeOfferToDutchAuction(MakeOfferInput input)
        {
            var auctionInfo = State.DutchAuctionInfoMap[input.Symbol][input.TokenId];
            if (auctionInfo == null)
            {
                throw new AssertionException($"Auction info of {input.Symbol}-{input.TokenId} not found.");
            }

            var duration = auctionInfo.Duration;
            if (Context.CurrentBlockTime < duration.StartTime)
            {
                PerformMakeOffer(input);
                return false;
            }

            Assert(Context.CurrentBlockTime <= duration.StartTime.AddHours(duration.DurationHours),
                "Auction already finished.");
            Assert(input.Price.Symbol == auctionInfo.PurchaseSymbol, "Incorrect symbol");
            var currentBiddingPrice = CalculateCurrentBiddingPrice(auctionInfo.StartingPrice, auctionInfo.EndingPrice,
                auctionInfo.Duration);
            if (input.Price.Amount < currentBiddingPrice)
            {
                PerformMakeOffer(input);
                return false;
            }

            PerformDeal(new PerformDealInput
            {
                NFTFrom = auctionInfo.Owner,
                NFTTo = Context.Sender,
                NFTQuantity = 1,
                NFTSymbol = input.Symbol,
                NFTTokenId = input.TokenId,
                PurchaseSymbol = input.Price.Symbol,
                PurchaseAmount = input.Price.Amount,
                PurchaseTokenId = 0
            });
            return true;
        }

        private long CalculateCurrentBiddingPrice(long startingPrice, long endingPrice, ListDuration duration)
        {
            var passedSeconds = (Context.CurrentBlockTime - duration.StartTime).Seconds;
            var durationSeconds = duration.DurationHours.Mul(3600);
            if (passedSeconds == 0)
            {
                return startingPrice;
            }

            var diffPrice = endingPrice.Sub(startingPrice);
            return Math.Max(startingPrice.Sub(diffPrice.Mul(durationSeconds).Div(passedSeconds)), endingPrice);
        }

        private void MaybeReceiveRemainDeposit(RequestInfo requestInfo)
        {
            if (requestInfo == null) return;
            Assert(Context.CurrentBlockTime > requestInfo.WhiteListDueTime, "Due time not passed.");
            var nftProtocolInfo =
                State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = requestInfo.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can claim remain deposit.");

            var nftVirtualAddressFrom = CalculateTokenHash(requestInfo.Symbol, requestInfo.TokenId);
            var nftVirtualAddress = Context.ConvertVirtualAddressToContractAddress(nftVirtualAddressFrom);
            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = requestInfo.Price.Symbol,
                Owner = nftVirtualAddress
            }).Balance;
            if (balance > 0)
            {
                State.TokenContract.Transfer.VirtualSend(nftVirtualAddressFrom, new TransferInput
                {
                    To = nftProtocolInfo.Creator,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = balance
                });
            }

            MaybeRemoveRequest(requestInfo.Symbol, requestInfo.TokenId);
        }

        public override Empty MintBadge(MintBadgeInput input)
        {
            var protocol = State.NFTContract.GetNFTProtocolInfo.Call(new StringValue {Value = input.Symbol});
            Assert(!string.IsNullOrWhiteSpace(protocol.Symbol), $"Protocol {input.Symbol} not found.");
            Assert(protocol.NftType.ToUpper() == NFTType.Badges.ToString().ToUpper(),
                "This method is only for badges.");
            var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });
            Assert(nftInfo.TokenId > 0, "Badge not found.");
            Assert(nftInfo.Metadata.Value.ContainsKey(BadgeMintWhitelistIdMetadataKey),
                $"Metadata {BadgeMintWhitelistIdMetadataKey} not found.");
            var whitelistIdHex = nftInfo.Metadata.Value[BadgeMintWhitelistIdMetadataKey];
            Assert(!string.IsNullOrWhiteSpace(whitelistIdHex),$"No whitelist.{whitelistIdHex}");
            var whitelistId = Hash.LoadFromHex(whitelistIdHex);
            //Whether NFT Market Contract is the manager.
            var isManager = State.WhitelistContract.GetManagerExistFromWhitelist.Call(new GetManagerExistFromWhitelistInput()
            {
                WhitelistId = whitelistId,
                Manager = Context.Self
            });
            Assert(isManager.Value == true,"NFT Market Contract does not in the manager list.");
            // Is Context.Sender in whitelist
            var ifExist = State.WhitelistContract.GetAddressFromWhitelist.Call(new GetAddressFromWhitelistInput()
            {
                WhitelistId = whitelistId,
                Address = Context.Sender
            });
            Assert(ifExist.Value,$"No permission.{Context.Sender}");
            State.NFTContract.Mint.Send(new MintInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Owner = Context.Sender,
                Quantity = 1
            });
            State.WhitelistContract.RemoveAddressInfoListFromWhitelist.Send(new RemoveAddressInfoListFromWhitelistInput()
            {
                WhitelistId = whitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { new ExtraInfoId()
                    {
                        AddressList = new Whitelist.AddressList(){Value = { Context.Sender }}
                    } }
                }
            });
            return new Empty();
        }
    }
}