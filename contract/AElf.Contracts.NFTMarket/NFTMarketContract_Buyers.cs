using System;
using System.Linq;
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using GetBalanceInput = AElf.Contracts.MultiToken.GetBalanceInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        /// <summary>
        /// There are 2 types of making offer.
        /// 1. Aiming a owner.
        /// 2. Only aiming nft. Owner will be null.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty MakeOffer(MakeOfferInput input)
        {
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

            if (nftInfo.Quantity == 0 && input.Quantity == 1)
            {
                // NFT not minted.
                PerformRequestNewItem(input.Symbol, input.TokenId, input.Price, input.ExpireTime, input.DueTime);
                return new Empty();
            }

            var listedNftInfo = State.ListedNftInfoMap[input.Symbol][input.TokenId][input.OfferTo];
            if (listedNftInfo == null || listedNftInfo.ListType == ListType.NotListed)
            {
                // NFT not listed by the owner.
                PerformMakeOffer(input);
                return new Empty();
            }

            Assert(listedNftInfo.Price.Symbol == input.Price.Symbol, "Symbol not match.");

            var quantity = input.Quantity;
            if (quantity > listedNftInfo.Quantity)
            {
                var makerOfferInput = input.Clone();
                makerOfferInput.Quantity = quantity.Sub(listedNftInfo.Quantity);
                PerformMakeOffer(makerOfferInput);
                input.Quantity = listedNftInfo.Quantity;
            }

            var whiteListAddressPriceList = State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId];
            switch (listedNftInfo.ListType)
            {
                case ListType.FixedPrice when input.Price.Amount >= listedNftInfo.Price.Amount:
                    TryDealWithFixedPrice(input, listedNftInfo);
                    break;
                case ListType.FixedPrice when whiteListAddressPriceList != null &&
                                              whiteListAddressPriceList.Value.Any(p => p.Address == Context.Sender):
                    TryDealWithFixedPrice(input, listedNftInfo);
                    State.RequestInfoMap[input.Symbol].Remove(input.TokenId);
                    break;
                case ListType.FixedPrice:
                    PerformMakeOffer(input);
                    break;
                case ListType.EnglishAuction:
                    PerformPlaceBidForEnglishAuction(input);
                    break;
                case ListType.DutchAuction:
                    PerformPlaceBidForDutchAuction(input);
                    break;
            }

            return new Empty();
        }

        public override Empty CancelOffer(CancelOfferInput input)
        {
            OfferList offerList;
            var newOfferList = new OfferList();
            // Admin can remove expired offer.
            if (input.OfferFrom != null)
            {
                AssertSenderIsAdmin();
                offerList = State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom];
                foreach (var offer in offerList.Value)
                {
                    if (offer.ExpireTime >= Context.CurrentBlockTime)
                    {
                        newOfferList.Value.Add(offer);
                    }
                }

                State.OfferListMap[input.Symbol][input.TokenId][input.OfferFrom] = newOfferList;
                return new Empty();
            }

            offerList = State.OfferListMap[input.Symbol][input.TokenId][Context.Sender];

            // Check Request Map first.
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                CancelRequest(input, requestInfo);
                // Only one request for each token id.
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

            // Check ListType.
            var offerTo = input.OfferTo ?? nftInfo.Creator;
            var listedNftInfo = State.ListedNftInfoMap[input.Symbol][input.TokenId][offerTo];

            switch (listedNftInfo.ListType)
            {
                case ListType.FixedPrice:
                    for (var i = 0; i < offerList.Value.Count; i++)
                    {
                        if (!input.IndexList.Value.Contains(i))
                        {
                            newOfferList.Value.Add(offerList.Value[i]);
                        }
                    }

                    break;
            }

            State.OfferListMap[input.Symbol][input.TokenId][Context.Sender] = newOfferList;

            return new Empty();
        }

        private void CancelRequest(CancelOfferInput input, RequestInfo requestInfo)
        {
            Assert(requestInfo.Requester == Context.Sender, "No permission.");
            var virtualAddress = CalculateNFTVirtuaAddress(input.Symbol, input.TokenId);
            var balanceOfNftVirtualAddress = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = requestInfo.Price.Symbol,
                Owner = virtualAddress
            }).Balance;

            if (requestInfo.IsConfirmed &&
                requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours) < Context.CurrentBlockTime)
            {
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

            var virtualAddressFrom = CalculateTokenHash(input.Symbol, input.TokenId);

            State.TokenContract.Transfer.VirtualSend(virtualAddressFrom, new TransferInput
            {
                To = requestInfo.Requester,
                Symbol = requestInfo.Price.Symbol,
                Amount = balanceOfNftVirtualAddress
            });
            State.RequestInfoMap[input.Symbol].Remove(input.TokenId);
        }

        /// <summary>
        /// Sender is buyer.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="listedNftInfo"></param>
        private void TryDealWithFixedPrice(MakeOfferInput input, ListedNFTInfo listedNftInfo)
        {
            var amount = listedNftInfo.Price.Amount;
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                if (listedNftInfo.Duration.PublicTime > Context.CurrentBlockTime)
                {
                    var whiteList = State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId];
                    var price = whiteList.Value.FirstOrDefault(p => p.Address == Context.Sender);
                    if (price != null)
                    {
                        Assert(input.Price.Symbol == price.Price.Symbol,
                            $"Need to use token {price.Price.Symbol}, not {input.Price.Symbol}");
                        amount = price.Price.Amount;
                    }
                    else
                    {
                        throw new AssertionException(
                            $"Sender is not in the white list, please need until {listedNftInfo.Duration.PublicTime}");
                    }
                }
            }

            var totalAmount = amount.Mul(input.Quantity);
            PerformDeal(new PerformDealInput
            {
                NFTFrom = input.OfferTo,
                NFTTo = Context.Sender,
                NFTSymbol = input.Symbol,
                NFTTokenId = input.TokenId,
                NFTQuantity = input.Quantity,
                PurchaseSymbol = input.Price.Symbol,
                PurchaseAmount = totalAmount
            });
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
                o.ExpireTime == expireTime);
            if (maybeSameOffer == null)
            {
                offerList.Value.Add(new Offer
                {
                    From = Context.Sender,
                    Price = input.Price,
                    ExpireTime = expireTime,
                    Quantity = input.Quantity
                });
            }
            else
            {
                maybeSameOffer.Quantity = maybeSameOffer.Quantity.Add(input.Quantity);
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
                OfferMaker = Context.Sender,
                OfferTo = input.OfferTo,
                ExpireTime = expireTime,
                Price = input.Price,
                Quantity = input.Quantity
            });
        }

        private void PerformPlaceBidForEnglishAuction(MakeOfferInput input)
        {
            var auctionInfo = State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];
            if (auctionInfo == null)
            {
                throw new AssertionException($"Auction info of {input.Symbol}-{input.TokenId} not found.");
            }

            var duration = auctionInfo.Duration;
            Assert(Context.CurrentBlockTime <= duration.StartTime.AddHours(duration.DurationHours),
                "Auction already finished.");
            Assert(input.Price.Symbol == auctionInfo.PurchaseSymbol, "Incorrect symbol");
            if (input.Price.Amount < auctionInfo.StartingPrice)
            {
                PerformMakeOffer(input);
                return;
            }

            var bidList = GetBidList(new GetOfferListInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            }) ?? new OfferList();
            var sortedBitList = new OfferList
            {
                Value =
                {
                    bidList.Value.OrderByDescending(o => o.Price.Amount)
                }
            };
            if (sortedBitList.Value.Any() && input.Price.Amount < sortedBitList.Value.First().Price.Amount)
            {
                PerformMakeOffer(input);
                return;
            }

            var newBid = new Offer
            {
                From = Context.Sender,
                Price = new Price
                {
                    Symbol = input.Price.Symbol,
                    Amount = input.Price.Amount
                },
                Quantity = 1,
                DueTime = input.DueTime ?? Context.CurrentBlockTime.AddDays(DefaultExpireDays),
                ExpireTime = input.ExpireTime ?? Context.CurrentBlockTime.AddDays(DefaultExpireDays)
            };

            var offerAddressList = State.BidAddressListMap[input.Symbol][input.TokenId] ?? new AddressList();
            if (!offerAddressList.Value.Contains(Context.Sender))
            {
                offerAddressList.Value.Add(Context.Sender);
                State.BidAddressListMap[input.Symbol][input.TokenId] = offerAddressList;
            }

            var senderOfferList = State.BidListMap[input.Symbol][input.TokenId][Context.Sender] ?? new OfferList();
            senderOfferList.Value.Add(newBid);
            State.BidListMap[input.Symbol][input.TokenId][Context.Sender] = senderOfferList;
        }

        private void PerformPlaceBidForDutchAuction(MakeOfferInput input)
        {
            var auctionInfo = State.DutchAuctionInfoMap[input.Symbol][input.TokenId];
            if (auctionInfo == null)
            {
                throw new AssertionException($"Auction info of {input.Symbol}-{input.TokenId} not found.");
            }

            var duration = auctionInfo.Duration;
            Assert(Context.CurrentBlockTime <= duration.StartTime.AddHours(duration.DurationHours),
                "Auction already finished.");
            Assert(input.Price.Symbol == auctionInfo.PurchaseSymbol, "Incorrect symbol");
            var currentBiddingPrice = CalculateCurrentBiddingPrice(auctionInfo.StartingPrice, auctionInfo.EndingPrice,
                auctionInfo.Duration);
            if (input.Price.Amount < currentBiddingPrice)
            {
                PerformMakeOffer(input);
            }
            else
            {
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
            }
        }

        private long CalculateCurrentBiddingPrice(long startingPrice, long endingPrice, ListDuration duration)
        {
            var passedHours = (Context.CurrentBlockTime - duration.StartTime).Seconds.Mul(3600);
            if (passedHours == 0)
            {
                return startingPrice;
            }

            var diffPrice = endingPrice.Sub(startingPrice);
            return startingPrice.Sub(diffPrice.Mul(duration.DurationHours).Div(passedHours));
        }
    }
}