using System.Linq;
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
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
            if (input.OriginOwner == null)
            {
                PerformMakeOffer(input);
                return new Empty();
            }

            Assert(Context.Sender != input.OriginOwner, "Origin owner cannot be sender himself.");

            var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });
            if (nftInfo.Quantity == 0)
            {
                // Not minted.
                PerformRequestNewItem(input.Symbol, input.TokenId);
                return new Empty();
            }

            var listedNftInfo = State.ListedNftInfoMap[input.Symbol][input.TokenId][input.OriginOwner];
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

            switch (listedNftInfo.ListType)
            {
                case ListType.FixedPrice when input.Price.Amount >= listedNftInfo.Price.Amount:
                    TryDealWithFixedPrice(input, listedNftInfo);
                    break;
                case ListType.FixedPrice:
                    PerformMakeOffer(input);
                    break;
                case ListType.EnglishAuction:
                    break;
                case ListType.DutchAuction:
                    break;
            }

            return new Empty();
        }

        public override Empty CancelOffer(CancelOfferInput input)
        {
            // Check Request Map first.
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                Assert(requestInfo.Requester == Context.Sender, "No permission.");
                if (requestInfo.IsConfirmed &&
                    requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours) >= Context.CurrentBlockTime)
                {
                    throw new AssertionException("Cannot cancel offer before due time.");
                }

                var virtualAddressFrom = CalculateTokenHash(input.Symbol, input.TokenId);
                State.TokenContract.Transfer.VirtualSend(virtualAddressFrom, new TransferInput
                {
                    To = requestInfo.Requester,
                    Symbol = requestInfo.Symbol,
                    Amount = requestInfo.Deposit
                });
                State.RequestInfoMap[input.Symbol].Remove(input.TokenId);
                return new Empty();
            }

            return new Empty();
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
                        Assert(input.Symbol == price.Price.Symbol, $"Need to use token {price.Price.Symbol}");
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
                NFTFrom = input.OriginOwner,
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
                OriginOwner = input.OriginOwner,
                ExpireTime = expireTime,
                Price = input.Price,
                Quantity = input.Quantity
            });
        }

        private void PerformPlaceABidForEnglishAuction(MakeOfferInput input)
        {

        }

        private void PerformPlaceABidForDutchAuction(MakeOfferInput input)
        {

        }
    }
}