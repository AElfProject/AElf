using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override ListedNFTInfoList GetListedNFTInfoList(GetListedNFTInfoListInput input)
        {
            return State.ListedNFTInfoListMap[input.Symbol][input.TokenId][input.Owner];
        }

        public override WhiteListAddressPriceList GetWhiteListAddressPriceList(GetWhiteListAddressPriceListInput input)
        {
            return State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId];
        }

        public override AddressList GetOfferAddressList(GetAddressListInput input)
        {
            return State.OfferAddressListMap[input.Symbol][input.TokenId];
        }

        public override OfferList GetOfferList(GetOfferListInput input)
        {
            if (input.Address != null)
            {
                return State.OfferListMap[input.Symbol][input.TokenId][input.Address];
            }

            var addressList = GetOfferAddressList(new GetAddressListInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            }) ?? new AddressList();
            var allOfferList = new OfferList();
            foreach (var address in addressList.Value)
            {
                var offerList = State.OfferListMap[input.Symbol][input.TokenId][address];
                if (offerList != null)
                {
                    allOfferList.Value.Add(offerList.Value);
                }
            }

            return allOfferList;
        }

        public override AddressList GetBidAddressList(GetAddressListInput input)
        {
            return State.BidAddressListMap[input.Symbol][input.TokenId];
        }

        public override Bid GetBid(GetBidInput input)
        {
            return State.BidMap[input.Symbol][input.TokenId][input.Address];
        }

        public override BidList GetBidList(GetBidListInput input)
        {
            var addressList = GetBidAddressList(new GetAddressListInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            }) ?? new AddressList();
            var allBidList = new BidList();
            foreach (var address in addressList.Value)
            {
                var bid = State.BidMap[input.Symbol][input.TokenId][address];
                if (bid != null)
                {
                    allBidList.Value.Add(bid);
                }
            }

            return allBidList;
        }

        public override CustomizeInfo GetCustomizeInfo(StringValue input)
        {
            return State.CustomizeInfoMap[input.Value];
        }

        public override RequestInfo GetRequestInfo(GetRequestInfoInput input)
        {
            return State.RequestInfoMap[input.Symbol][input.TokenId];
        }

        public override EnglishAuctionInfo GetEnglishAuctionInfo(GetEnglishAuctionInfoInput input)
        {
            return State.EnglishAuctionInfoMap[input.Symbol][input.TokenId];
        }

        public override DutchAuctionInfo GetDutchAuctionInfo(GetDutchAuctionInfoInput input)
        {
            return State.DutchAuctionInfoMap[input.Symbol][input.TokenId];
        }

        public override StringList GetTokenWhiteList(StringValue input)
        {
            return State.TokenWhiteListMap[input.Value];
        }

        public override StringList GetGlobalTokenWhiteList(Empty input)
        {
            return State.GlobalTokenWhiteList.Value;
        }

        public override Price GetStakingTokens(StringValue input)
        {
            var customizeInfo = State.CustomizeInfoMap[input.Value];
            if (customizeInfo == null)
            {
                return new Price();
            }

            return new Price
            {
                Symbol = customizeInfo.Price.Symbol,
                Amount = customizeInfo.StakingAmount
            };
        }
    }
}