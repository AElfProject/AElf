using AElf.Contracts.NFT;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty SetRoyalty(SetRoyaltyInput input)
        {
            // 0% - 10%
            Assert(0 <= input.Royalty && input.Royalty <= 1000);
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            if (input.TokenId == 0)
            {
                Assert(nftProtocolInfo.Creator == Context.Sender,
                    "Only NFT Protocol Creator can set royalty for whole protocol.");
                // Set for whole NFT Protocol.
                State.RoyaltyMap[input.Symbol] = input.Royalty;
            }
            else
            {
                var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId
                });
                Assert(nftProtocolInfo.Creator == Context.Sender || nftInfo.Minters.Contains(Context.Sender),
                    "No permission.");
                State.CertainNFTRoyaltyMap[input.Symbol][input.TokenId] = input.Royalty;
            }

            State.RoyaltyFeeReceiverMap[input.Symbol] = input.RoyaltyFeeReceiver;
            return new Empty();
        }

        public override Empty SetTokenWhiteList(SetTokenWhiteListInput input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can set token white list.");
            State.TokenWhiteListMap[input.Symbol] = input.TokenWhiteList;
            return new Empty();
        }

        public override Empty SetFloorPrice(SetFloorPriceInput input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can set floor prices.");
            var tokenWhiteList = GetTokenWhiteList(input.Symbol);
            foreach (var pair in input.FloorPrices)
            {
                Assert(tokenWhiteList.Value.Contains(pair.Key),
                    $"{pair.Key} is not in the token white list of protocol {input.Symbol}.");
                Assert(pair.Value >= 0, "Floor price should not be negative.");
                State.FloorPriceMap[input.Symbol][pair.Key] = pair.Value;
            }

            return new Empty();
        }

        public override Empty SetCustomizeInfo(CustomizeInfo input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can set customize info.");
            State.CustomizeInfoMap[input.Symbol] = input;
            return new Empty();
        }

        public override Empty HandleRequest(HandleRequestInput input)
        {
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo == null)
            {
                throw new AssertionException("Request not exists.");
            }

            if (input.IsConfirm)
            {
                requestInfo.IsConfirmed = true;
                requestInfo.ConfirmTime = Context.CurrentBlockTime;
                State.RequestInfoMap[input.Symbol][input.TokenId] = requestInfo;
                Context.Fire(new NewNFTRequestConfirmed
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Requester = input.Requester
                });
            }
            else
            {
                State.RequestInfoMap[input.Symbol].Remove(input.TokenId);
                Context.Fire(new NewNFTRequestRejected
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Requester = input.Requester
                });
            }

            return new Empty();
        }
    }
}