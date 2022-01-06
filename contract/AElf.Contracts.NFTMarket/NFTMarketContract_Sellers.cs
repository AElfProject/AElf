using System;
using System.Linq;
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
            var duration = input.Duration;
            if (duration == null)
            {
                duration = new Duration
                {
                    StartTime = Context.CurrentBlockTime,
                    PublicTime = Context.CurrentBlockTime,
                    DurationHours = int.MaxValue
                };
            }
            else
            {
                if (duration.StartTime == null || duration.StartTime > Context.CurrentBlockTime)
                {
                    duration.StartTime = Context.CurrentBlockTime;
                }

                if (duration.DurationHours == 0)
                {
                    duration.DurationHours = int.MaxValue;
                }
            }

            var whiteListAddressPriceList = input.WhiteListAddressPriceList;
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo != null)
            {
                Assert(
                    whiteListAddressPriceList.Value.Count == 1 &&
                    whiteListAddressPriceList.Value.Any(p => p.Address == requestInfo.Requester),
                    "Incorrect white list address price list.");
                Assert(input.Price.Symbol == requestInfo.Symbol, $"Need to use token {requestInfo.Symbol}");

                var supposedPublicTime1 = Context.CurrentBlockTime.AddHours(requestInfo.WhiteListHours);
                var supposedPublicTime2 = requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours)
                    .AddHours(requestInfo.WhiteListHours);
                Assert(
                    input.Duration.PublicTime >= supposedPublicTime1 &&
                    input.Duration.PublicTime >= supposedPublicTime2, "Incorrect white list hours.");

                whiteListAddressPriceList.Value[0].Price.Amount = Math.Min(input.Price.Amount,
                    Math.Min(requestInfo.Balance, whiteListAddressPriceList.Value[0].Price.Amount));
                requestInfo.ListTime = Context.CurrentBlockTime;
                State.RequestInfoMap[input.Symbol][input.TokenId] = requestInfo;
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
            State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId] = whiteListAddressPriceList;
            Context.Fire(new ListedNFTInfoChanged
            {
                ListType = listedNftInfo.ListType,
                Owner = listedNftInfo.Owner,
                Price = listedNftInfo.Price,
                Quantity = listedNftInfo.Quantity,
                Symbol = listedNftInfo.Symbol,
                TokenId = listedNftInfo.TokenId,
                Duration = listedNftInfo.Duration,
                Description = input.Description
            });
            return new Empty();
        }

        public override Empty ListWithEnglishAuction(ListWithEnglishAuctionInput input)
        {
            Assert(CanBeListedWithAuction(input.Symbol, input.TokenId), "This NFT cannot be listed with auction for now.");
            return new Empty();
        }

        public override Empty ListWithDutchAuction(ListWithDutchAuctionInput input)
        {
            Assert(CanBeListedWithAuction(input.Symbol, input.TokenId), "This NFT cannot be listed with auction for now.");
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
    }
}