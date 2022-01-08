using System;
using System.Linq;
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty ListWithFixedPrice(ListWithFixedPriceInput input)
        {
            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, input.Quantity);
            var duration = AdjustListDuration(input.Duration);
            var whiteListAddressPriceList = input.WhiteListAddressPriceList;
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                ListRequestedNFT(input, requestInfo, whiteListAddressPriceList);
            }

            var listedNftInfo = new ListedNFTInfo
            {
                ListType = ListType.FixedPrice,
                Owner = Context.Sender,
                Price = input.Price,
                Quantity = input.Quantity,
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = duration,
            };
            State.ListedNftInfoMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfo;
            if (whiteListAddressPriceList != null)
            {
                State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId] = whiteListAddressPriceList;
            }

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
            var duration = AdjustListDuration(input.Duration);

            var englishAuctionInfo = new EnglishAuctionInfo
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = duration,
                PurchaseSymbol = input.PurchaseSymbol,
                StartingPrice = input.StartingPrice,
                Owner = Context.Sender
            };
            State.EnglishAuctionInfoMap[input.Symbol][input.TokenId] = englishAuctionInfo;
            
            var whiteListAddressPriceList = input.WhiteListAddressPriceList;
            if (whiteListAddressPriceList != null)
            {
                State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId] = whiteListAddressPriceList;
            }

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
            var listedNftInfo = State.ListedNftInfoMap[input.Symbol][input.TokenId][Context.Sender];
            if (listedNftInfo == null || listedNftInfo.ListType == ListType.NotListed)
            {
                throw new AssertionException("Listed NFT Info not exists. (Or already delisted.)");
            }

            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                if (requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours) < Context.CurrentBlockTime)
                {
                    throw new AssertionException("Cannot delist this NFT.");
                }

                requestInfo.ListTime = null;
                State.RequestInfoMap[input.Symbol][input.TokenId] = requestInfo;
            }

            switch (listedNftInfo.ListType)
            {
                case ListType.FixedPrice when input.Quantity >= listedNftInfo.Quantity:
                    State.ListedNftInfoMap[input.Symbol][input.TokenId].Remove(Context.Sender);
                    break;
                case ListType.FixedPrice:
                    listedNftInfo.Quantity = listedNftInfo.Quantity.Sub(input.Quantity);
                    State.ListedNftInfoMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfo;
                    break;
                case ListType.EnglishAuction:
                    break;
                case ListType.DutchAuction:
                    break;
            }

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
            Assert(input.OfferMaker != null, "Incorrect offer maker.");
            Assert(input.Price?.Symbol != null, "Incorrect price.");

            var balance = State.NFTContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Owner = Context.Sender
            });
            Assert(balance.Balance >= input.Quantity, "Insufficient balance.");

            var offer = State.OfferListMap[input.Symbol][input.TokenId][input.OfferMaker].Value
                .FirstOrDefault(o => o.From == input.OfferMaker && o.Price.Symbol == input.Price.Symbol &&
                                     o.Price.Amount == input.Price.Amount);
            if (offer == null)
            {
                throw new AssertionException("Related offer not found.");
            }

            Assert(offer.Quantity >= input.Quantity, "Offer quantity exceeded.");
            var totalAmount = offer.Price.Amount.Mul(input.Quantity);
            PerformDeal(new PerformDealInput
            {
                NFTFrom = Context.Sender,
                NFTTo = offer.From,
                NFTSymbol = input.Symbol,
                NFTTokenId = input.TokenId,
                NFTQuantity = input.Quantity,
                PurchaseSymbol = offer.Price.Symbol,
                PurchaseAmount = totalAmount
            });
            return new Empty();
        }
    }
}