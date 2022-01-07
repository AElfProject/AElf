using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override ListedNFTInfo GetListedNFTInfo(GetListedNFTInfoInput input)
        {
            return State.ListedNftInfoMap[input.Symbol][input.TokenId][input.Owner];
        }

        public override WhiteListAddressPriceList GetWhiteListAddressPriceList(GetWhiteListAddressPriceListInput input)
        {
            return State.WhiteListAddressPriceListMap[input.Symbol][input.TokenId];
        }

        public override AddressList GetOfferAddressList(GetOfferAddressListInput input)
        {
            return State.OfferAddressListMap[input.Symbol][input.TokenId];
        }

        public override OfferList GetOfferList(GetOfferListInput input)
        {
            if (input.Address != null)
            {
                return State.OfferListMap[input.Symbol][input.TokenId][input.Address];
            }

            var addressList = GetOfferAddressList(new GetOfferAddressListInput
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId
            });
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

        public override CustomizeInfo GetCustomizeInfo(StringValue input)
        {
            return State.CustomizeInfoMap[input.Value];
        }

        public override RequestInfo GetRequestInfo(GetRequestInfoInput input)
        {
            return State.RequestInfoMap[input.Symbol][input.TokenId];
        }
    }
}