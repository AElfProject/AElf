using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.NFT;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        private void CheckSenderNFTBalanceAndAllowance(string symbol, long tokenId, long quantity)
        {
            var balance = State.NFTContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Owner = Context.Sender
            }).Balance;
            Assert(balance >= quantity, $"Check sender NFT balance failed. {balance} / {quantity}");
            var allowance = State.NFTContract.GetAllowance.Call(new GetAllowanceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Owner = Context.Sender,
                Spender = Context.Self
            }).Allowance;
            Assert(allowance >= quantity, $"Check sender NFT allowance failed. {allowance} / {quantity}");
        }

        private bool CheckAllowanceAndBalanceIsEnough(Address owner, string symbol, long enoughAmount)
        {
            var balance = State.TokenContract.GetBalance.Call(new MultiToken.GetBalanceInput
            {
                Symbol = symbol,
                Owner = owner
            }).Balance;
            if (balance < enoughAmount) return false;
            var allowance = State.TokenContract.GetAllowance.Call(new MultiToken.GetAllowanceInput
            {
                Symbol = symbol,
                Owner = owner,
                Spender = Context.Self
            }).Allowance;
            return allowance >= enoughAmount;
        }

        private void PayRemainDepositInCustomizeCase(PerformDealInput performDealInput)
        {
            var requestInfo = State.RequestInfoMap[performDealInput.NFTSymbol][performDealInput.NFTTokenId];
            if (requestInfo == null) return;
            var nftVirtualAddressFrom = CalculateTokenHash(performDealInput.NFTSymbol, performDealInput.NFTTokenId);
            var nftVirtualAddress = Context.ConvertVirtualAddressToContractAddress(nftVirtualAddressFrom);
            var balanceOfNftVirtualAddress = State.TokenContract.GetBalance.Call(new MultiToken.GetBalanceInput
            {
                Symbol = performDealInput.PurchaseSymbol,
                Owner = nftVirtualAddress
            }).Balance;
            var transferAmount = balanceOfNftVirtualAddress;
            var serviceFee = transferAmount.Mul(State.ServiceFeeRate.Value).Div(FeeDenominator);
            transferAmount = transferAmount.Sub(serviceFee);
            if (transferAmount > 0)
            {
                State.TokenContract.Transfer.VirtualSend(nftVirtualAddressFrom, new TransferInput
                {
                    To = performDealInput.NFTFrom,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = transferAmount
                });
            }

            if (serviceFee > 0)
            {
                State.TokenContract.Transfer.VirtualSend(nftVirtualAddressFrom, new TransferInput
                {
                    To = State.ServiceFeeReceiver.Value,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = serviceFee
                });
            }
        }

        private void PerformDeal(PerformDealInput performDealInput)
        {
            Assert(performDealInput.NFTFrom != performDealInput.NFTTo, "NFT From address cannot be NFT To address.");
            PayRemainDepositInCustomizeCase(performDealInput);
            if (performDealInput.PurchaseTokenId == 0)
            {
                var serviceFee = performDealInput.PurchaseAmount.Mul(State.ServiceFeeRate.Value).Div(FeeDenominator);
                var royalty = GetRoyalty(new GetRoyaltyInput
                {
                    Symbol = performDealInput.NFTSymbol,
                    TokenId = performDealInput.NFTTokenId
                });

                var royaltyFee = performDealInput.PurchaseAmount.Mul(royalty.Royalty).Div(FeeDenominator);
                var royaltyFeeReceiver = State.RoyaltyFeeReceiverMap[performDealInput.NFTSymbol];
                if (royaltyFeeReceiver == null)
                {
                    royaltyFee = 0;
                }

                var actualAmount = performDealInput.PurchaseAmount.Sub(serviceFee).Sub(royaltyFee);
                //Assert(actualAmount > 0, "Incorrect deal amount.");
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = performDealInput.NFTTo,
                    To = performDealInput.NFTFrom,
                    Symbol = performDealInput.PurchaseSymbol,
                    Amount = actualAmount
                });
                if (serviceFee > 0)
                {
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = performDealInput.NFTTo,
                        To = State.ServiceFeeReceiver.Value,
                        Symbol = performDealInput.PurchaseSymbol,
                        Amount = serviceFee
                    });
                }
                if (royaltyFeeReceiver != null && royaltyFee > 0)
                {
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = performDealInput.NFTTo,
                        To = royaltyFeeReceiver,
                        Symbol = performDealInput.PurchaseSymbol,
                        Amount = royaltyFee
                    });
                }
            }
            else
            {
                // Exchange NFTs for NFTs.

                State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
                {
                    From = performDealInput.NFTTo,
                    To = performDealInput.NFTFrom,
                    Symbol = performDealInput.PurchaseSymbol,
                    TokenId = performDealInput.PurchaseTokenId,
                    Amount = performDealInput.PurchaseAmount
                });

                if (State.ServiceFee.Value > 0)
                {
                    // Charge a fixed service fee.
                    State.TokenContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = performDealInput.NFTTo,
                        To = State.ServiceFeeReceiver.Value,
                        Symbol = Context.Variables.NativeSymbol,
                        Amount = State.ServiceFee.Value
                    });
                }
            }

            State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
            {
                From = performDealInput.NFTFrom,
                To = performDealInput.NFTTo,
                Symbol = performDealInput.NFTSymbol,
                TokenId = performDealInput.NFTTokenId,
                Amount = performDealInput.NFTQuantity
            });

            Context.Fire(new Sold
            {
                NftFrom = performDealInput.NFTFrom,
                NftTo = performDealInput.NFTTo,
                NftSymbol = performDealInput.NFTSymbol,
                NftTokenId = performDealInput.NFTTokenId,
                NftQuantity = performDealInput.NFTQuantity,
                PurchaseSymbol = performDealInput.PurchaseSymbol,
                PurchaseTokenId = performDealInput.PurchaseTokenId,
                PurchaseAmount = performDealInput.PurchaseAmount
            });
        }

        private struct PerformDealInput
        {
            public Address NFTFrom { get; set; }
            public Address NFTTo { get; set; }
            public string NFTSymbol { get; set; }
            public long NFTTokenId { get; set; }
            public long NFTQuantity { get; set; }
            public string PurchaseSymbol { get; set; }

            /// <summary>
            /// If PurchaseSymbol is a Fungible token, PurchaseTokenIs shall always be 0.
            /// </summary>
            public long PurchaseTokenId { get; set; }

            /// <summary>
            /// Be aware of that this stands for total amount.
            /// </summary>
            public long PurchaseAmount { get; set; }
        }

        private StringList GetTokenWhiteList(string symbol)
        {
            var tokenWhiteList = State.TokenWhiteListMap[symbol] ?? State.GlobalTokenWhiteList.Value;
            foreach (var globalWhiteListSymbol in State.GlobalTokenWhiteList.Value.Value)
            {
                if (!tokenWhiteList.Value.Contains(globalWhiteListSymbol))
                {
                    tokenWhiteList.Value.Add(globalWhiteListSymbol);
                }
            }

            return tokenWhiteList;
        }

        private void PerformRequestNewItem(string symbol, long tokenId, Price price, Timestamp expireTime)
        {
            var customizeInfo = State.CustomizeInfoMap[symbol];
            if (customizeInfo?.Price == null || customizeInfo.Price.Amount == 0)
            {
                throw new AssertionException("Cannot request new item for this protocol.");
            }

            Assert(State.RequestInfoMap[symbol][tokenId] == null, "Request already existed.");

            var nftVirtualAddress = CalculateNFTVirtuaAddress(symbol, tokenId);
            var priceSymbol = customizeInfo.Price.Symbol;
            var priceAmount = price.Amount == 0
                ? customizeInfo.Price.Amount
                : Math.Max(price.Amount, customizeInfo.Price.Amount);
            Assert(price.Symbol == customizeInfo.Price.Symbol, "Incorrect symbol.");
            Assert(priceAmount >= customizeInfo.Price.Amount, "Incorrect amount.");
            // Check allowance.
            var allowance = State.TokenContract.GetAllowance.Call(new MultiToken.GetAllowanceInput
            {
                Symbol = priceSymbol,
                Owner = Context.Sender,
                Spender = Context.Self
            }).Allowance;
            Assert(allowance >= priceAmount, "Insufficient allowance.");

            var deposit = priceAmount.Mul(customizeInfo.DepositRate).Div(FeeDenominator);
            if (deposit > 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = nftVirtualAddress,
                    Symbol = priceSymbol,
                    Amount = deposit
                });
            }

            var defaultExpireTime = Context.CurrentBlockTime.AddDays(DefaultExpireDays);
            State.RequestInfoMap[symbol][tokenId] = new RequestInfo
            {
                Symbol = symbol,
                TokenId = tokenId,
                DepositRate = customizeInfo.DepositRate,
                Price = new Price
                {
                    Symbol = priceSymbol,
                    Amount = priceAmount
                },
                WhiteListHours = customizeInfo.WhiteListHours,
                WorkHoursFromCustomizeInfo = customizeInfo.WorkHours,
                Requester = Context.Sender,
                ExpireTime = expireTime ?? defaultExpireTime
            };

            customizeInfo.ReservedTokenIds.Add(tokenId);
            State.CustomizeInfoMap[symbol] = customizeInfo;

            Context.Fire(new NewNFTRequested
            {
                Symbol = symbol,
                Requester = Context.Sender,
                TokenId = tokenId
            });
        }

        private bool CanBeListedWithAuction(string symbol, long tokenId, out RequestInfo requestInfo)
        {
            requestInfo = State.RequestInfoMap[symbol][tokenId];

            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call(new StringValue {Value = symbol});
            if (nftProtocolInfo.IsTokenIdReuse)
            {
                return false;
            }

            var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
            {
                Symbol = symbol,
                TokenId = tokenId
            });
            if (nftInfo.Quantity != 1)
            {
                return false;
            }

            if (requestInfo != null)
            {
                if (requestInfo.IsConfirmed && requestInfo.ListTime == null)
                {
                    // Confirmed but never listed by fixed price.
                    return false;
                }

                var whitelistDueTime1 = requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours)
                    .AddHours(requestInfo.WhiteListHours);
                var whiteListDueTime2 = requestInfo.ListTime.AddHours(requestInfo.WhiteListHours);
                if (Context.CurrentBlockTime <= whitelistDueTime1 || Context.CurrentBlockTime <= whiteListDueTime2)
                {
                    return false;
                }
            }

            return true;
        }

        private Hash CalculateTokenHash(string symbol, long tokenId = 0)
        {
            return HashHelper.ComputeFrom($"{symbol}{tokenId}");
        }

        private Address CalculateNFTVirtuaAddress(string symbol, long tokenId = 0)
        {
            return Context.ConvertVirtualAddressToContractAddress(CalculateTokenHash(symbol, tokenId));
        }

        private ListDuration AdjustListDuration(ListDuration duration)
        {
            if (duration == null)
            {
                duration = new ListDuration
                {
                    StartTime = Context.CurrentBlockTime,
                    PublicTime = Context.CurrentBlockTime,
                    DurationHours = int.MaxValue
                };
            }
            else
            {
                if (duration.StartTime == null || duration.StartTime < Context.CurrentBlockTime)
                {
                    duration.StartTime = Context.CurrentBlockTime;
                }

                if (duration.PublicTime == null || duration.PublicTime < duration.StartTime)
                {
                    duration.PublicTime = duration.StartTime;
                }

                if (duration.DurationHours == 0)
                {
                    duration.DurationHours = int.MaxValue;
                }
            }

            return duration;
        }
        
        private void ListRequestedNFT(ListWithFixedPriceInput input, RequestInfo requestInfo,
            WhitelistInfoList whitelistInfo)
        {
            if (whitelistInfo == null)
            {
                throw new AssertionException("Incorrect white list address price list.");
            }
            var projectId = CalculateProjectId(input.Symbol, input.TokenId, Context.Sender);
            var whitelistId = State.WhitelistIdMap[projectId];
            //TODO:Whether to adjust whitelist info to be correct.
            Assert(whitelistInfo.Whitelists.Count == 1 &&
                   whitelistInfo.Whitelists.Any(p =>
                       p.AddressList.Value.Contains(requestInfo.Requester) && p.AddressList.Value.Count == 1),
                "Incorrect white list address price list.");
            Assert(input.Price.Symbol == requestInfo.Price.Symbol, $"Need to use token {requestInfo.Price.Symbol}");

            var supposedPublicTime1 = Context.CurrentBlockTime.AddHours(requestInfo.WhiteListHours);
            var supposedPublicTime2 = requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours)
                .AddHours(requestInfo.WhiteListHours);
            Assert(
                input.Duration.PublicTime >= supposedPublicTime1 &&
                input.Duration.PublicTime >= supposedPublicTime2, "Incorrect white list hours.");

            Assert(requestInfo.Price.Amount <= input.Price.Amount, "List price too low.");

            var whiteListRemainPrice =
                requestInfo.Price.Amount.Sub(requestInfo.Price.Amount.Mul(requestInfo.DepositRate)
                    .Div(FeeDenominator));
            var delayDuration = Context.CurrentBlockTime - requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours);
            if (delayDuration.Seconds > 0)
            {
                var reducePrice = whiteListRemainPrice.Mul(delayDuration.Seconds)
                    .Div(delayDuration.Seconds.Add(requestInfo.WorkHours.Mul(3600)));
                whiteListRemainPrice = whiteListRemainPrice.Sub(reducePrice);
            }

            whitelistInfo.Whitelists[0].PriceTag.Price.Amount = Math.Min(input.Price.Amount,
                Math.Min(whiteListRemainPrice, whitelistInfo.Whitelists[0].PriceTag.Price.Amount));
            var tagName = $"Requested {whitelistInfo.Whitelists[0].PriceTag.Price.Amount}{whitelistInfo.Whitelists[0].PriceTag.Price.Symbol}";
            if (whitelistId == null)
            {
                whitelistInfo.Whitelists[0].PriceTag.TagName = tagName;
                var extraInfoList = ConvertToExtraInfo(whitelistInfo);
                State.WhitelistContract.CreateWhitelist.Send(new CreateWhitelistInput()
                {
                    ProjectId = projectId,
                    StrategyType = StrategyType.Price,
                    Creator = Context.Self,
                    ExtraInfoList = extraInfoList,
                    IsCloneable = true,
                    Remark = $"{input.Symbol}{input.TokenId}",
                    ManagerList = new Whitelist.AddressList
                    {
                        Value = { Context.Sender }
                    }
                });
                whitelistId =
                    Context.GenerateId(State.WhitelistContract.Value,
                        ByteArrayHelper.ConcatArrays(Context.Self.ToByteArray(), projectId.ToByteArray()));
                State.WhitelistIdMap[projectId] = whitelistId;
            }
            else
            {
                var tagId = HashHelper.ComputeFrom(
                    $"{whitelistId}" +
                    $"{projectId}{tagName}");
                var ifExist = State.WhitelistContract.GetTagInfoFromWhitelist.Call(new GetTagInfoFromWhitelistInput()
                {
                    WhitelistId = whitelistId,
                    ProjectId = projectId,
                    TagInfo = new TagInfo
                    {
                        TagName = tagName,
                        Info = new PriceTag
                        {
                            Symbol = whitelistInfo.Whitelists[0].PriceTag.Price.Symbol,
                            Amount = whitelistInfo.Whitelists[0].PriceTag.Price.Amount
                        }.ToByteString()
                    }
                }).Value;
                if (ifExist)
                {
                    State.WhitelistContract.AddAddressInfoListToWhitelist.Send(new AddAddressInfoListToWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        ExtraInfoIdList = new ExtraInfoIdList
                        {
                            Value = { new ExtraInfoId
                            {
                                AddressList = new Whitelist.AddressList
                                {
                                    Value = {whitelistInfo.Whitelists[0].AddressList.Value.First()}
                                },
                                Id = tagId
                            } }
                        }
                    });
                }
                else
                {
                    State.WhitelistContract.AddExtraInfo.Send(new AddExtraInfoInput()
                    {
                        WhitelistId = whitelistId,
                        ProjectId = projectId,
                        TagInfo = new TagInfo()
                        {
                            TagName = tagName,
                            Info = new PriceTag
                            {
                                Symbol = whitelistInfo.Whitelists[0].PriceTag.Price.Symbol,
                                Amount = whitelistInfo.Whitelists[0].PriceTag.Price.Amount
                            }.ToByteString()
                        },
                        AddressList = new Whitelist.AddressList()
                        {
                            Value = {whitelistInfo.Whitelists[0].AddressList.Value.First()}
                        }
                    });
                }
            }
            requestInfo.ListTime = Context.CurrentBlockTime;
            State.RequestInfoMap[input.Symbol][input.TokenId] = requestInfo;
        }

        private void ClearBids(string symbol, long tokenId, Address except = null)
        {
            var bidAddressList = State.BidAddressListMap[symbol][tokenId];
            var auctionInfo = State.EnglishAuctionInfoMap[symbol][tokenId];

            if (bidAddressList == null || !bidAddressList.Value.Any() || auctionInfo == null) return;

            if (except != null)
            {
                bidAddressList.Value.Remove(except);
            }

            var transferredEarnestMoney = 0L;
            foreach (var bidAddress in bidAddressList.Value)
            {
                if (auctionInfo.EarnestMoney > 0)
                {
                    var earnestMoneyReceiver = State.BidMap[symbol][tokenId] == null ? auctionInfo.Owner : bidAddress;
                    State.TokenContract.Transfer.VirtualSend(CalculateTokenHash(symbol, tokenId),
                        new TransferInput
                        {
                            To = earnestMoneyReceiver,
                            Symbol = auctionInfo.PurchaseSymbol,
                            Amount = auctionInfo.EarnestMoney
                        });
                    transferredEarnestMoney = transferredEarnestMoney.Add(auctionInfo.EarnestMoney);
                }

                State.BidMap[symbol][tokenId].Remove(bidAddress);

                Context.Fire(new BidCanceled
                {
                    Symbol = symbol,
                    TokenId = tokenId,
                    BidFrom = bidAddress,
                    BidTo = Context.Sender,
                });
            }

            State.BidAddressListMap[symbol].Remove(tokenId);

            var virtualAddressBalance = State.TokenContract.GetBalance.Call(new MultiToken.GetBalanceInput
            {
                Owner = CalculateNFTVirtuaAddress(symbol, tokenId),
                Symbol = auctionInfo.PurchaseSymbol
            }).Balance;
            var remainAmount = virtualAddressBalance.Sub(transferredEarnestMoney);
            if (except != null)
            {
                remainAmount = remainAmount.Sub(auctionInfo.EarnestMoney.Mul(except.Value.Length));
            }
            if (remainAmount > 0)
            {
                State.TokenContract.Transfer.VirtualSend(CalculateTokenHash(symbol, tokenId),
                    new TransferInput
                    {
                        To = auctionInfo.Owner,
                        Symbol = auctionInfo.PurchaseSymbol,
                        Amount = remainAmount
                    });
            }
        }

        private void MaybeRemoveRequest(string symbol, long tokenId)
        {
            State.RequestInfoMap[symbol].Remove(tokenId);
            if (State.CustomizeInfoMap[symbol] != null && State.CustomizeInfoMap[symbol].ReservedTokenIds != null &&
                State.CustomizeInfoMap[symbol].ReservedTokenIds.Any())
            {
                if (State.CustomizeInfoMap[symbol].ReservedTokenIds.Contains(tokenId))
                {
                    State.CustomizeInfoMap[symbol].ReservedTokenIds.Remove(tokenId);
                }
            }
        }

        private bool IsListedNftTimedOut(ListedNFTInfo listedNftInfo)
        {
            var expireTime = listedNftInfo.Duration.StartTime.AddHours(listedNftInfo.Duration.DurationHours);
            return Context.CurrentBlockTime > expireTime;
        }

        private Price DeserializedInfo(TagInfo tagInfo)
        {
            var deserializedInfo = new PriceTag();
            deserializedInfo.MergeFrom(tagInfo.Info);
            return new Price
            {
                Symbol = deserializedInfo.Symbol,
                Amount = deserializedInfo.Amount
            };
        }

        private Hash CalculateProjectId(string symbol, long tokenId,Address sender)
        {
            return HashHelper.ComputeFrom($"{symbol}{tokenId}{sender}");
        }

        private ExtraInfoList ConvertToExtraInfo(WhitelistInfoList input)
        {
            var extraInfoList = new ExtraInfoList();
            if (input == null)
            {
                return extraInfoList;
            }
            foreach (var whitelist in input.Whitelists)
            {
                var extraInfo = new ExtraInfo
                {
                    AddressList = new Whitelist.AddressList {Value = {whitelist.AddressList.Value}},
                    Info = new TagInfo
                    {
                        TagName = whitelist.PriceTag.TagName,
                        Info = new PriceTag
                        {
                            Symbol = whitelist.PriceTag.Price.Symbol,
                            Amount = whitelist.PriceTag.Price.Amount
                        }.ToByteString()
                    }
                };
                extraInfoList.Value.Add(extraInfo);
            }

            return extraInfoList;
        }
        
         private void DealRequestInfoInWhitelist(ListWithFixedPriceInput input,ListDuration duration,RequestInfo requestInfo)
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
                ListRequestedNFT(input, requestInfo, input.Whitelists);
                duration.StartTime = Context.CurrentBlockTime;
            }
            else
            {
                MaybeReceiveRemainDeposit(requestInfo);
            }
        }

        private Hash ExistWhitelist(Hash projectId, WhitelistInfoList whitelistInfoList, ExtraInfoList extraInfoList)
        {
            var whitelistManager = GetWhitelistManager();
            var whitelistId = State.WhitelistIdMap[projectId];
            //Format data.
            var extraInfoIdList = whitelistInfoList?.Whitelists.GroupBy(p => p.PriceTag)
                                .ToDictionary(e => e.Key, e => e.ToList())
                                .Select(extra =>
                                {
                                    //Whether price tag already exists.
                                    var ifExist = whitelistManager.GetTagInfoFromWhitelist(
                                        new GetTagInfoFromWhitelistInput()
                                        {
                                            ProjectId = projectId,
                                            WhitelistId = whitelistId,
                                            TagInfo = new TagInfo
                                            {
                                                TagName = extra.Key.TagName,
                                                Info = new PriceTag
                                                {
                                                    Symbol = extra.Key.Price.Symbol,
                                                    Amount = extra.Key.Price.Amount
                                                }.ToByteString()
                                            }
                                        });
                                    if (!ifExist)
                                    {
                                        //Doesn't exist,add tag info.
                                        whitelistManager.AddExtraInfo(new AddExtraInfoInput()
                                        {
                                            ProjectId = projectId,
                                            WhitelistId = whitelistId,
                                            TagInfo = new TagInfo
                                            {
                                                TagName = extra.Key.TagName,
                                                Info = new PriceTag
                                                {
                                                    Symbol = extra.Key.Price.Symbol,
                                                    Amount = extra.Key.Price.Amount
                                                }.ToByteString()
                                            }
                                        });
                                    }
                                    var tagId =
                                        HashHelper.ComputeFrom(
                                            $"{whitelistId}{projectId}{extra.Key.TagName}");
                                    var toAddExtraInfoIdList = new ExtraInfoIdList();
                                    foreach (var whitelistInfo in extra.Value.Where(whitelistInfo =>
                                                 whitelistInfo.AddressList.Value.Any()))
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
            if (extraInfoList == null || extraInfoIdList == null || extraInfoIdList.Count == 0) return whitelistId;
            {
                var toAdd = new ExtraInfoIdList();
                foreach (var extra in extraInfoIdList)
                {
                    toAdd.Value.Add(extra.Value);
                }
                whitelistManager.AddAddressInfoListToWhitelist(
                    new AddAddressInfoListToWhitelistInput()
                    {
                        WhitelistId = whitelistId,
                        ExtraInfoIdList = toAdd
                    });
            }
            return whitelistId;
        }
    }
}