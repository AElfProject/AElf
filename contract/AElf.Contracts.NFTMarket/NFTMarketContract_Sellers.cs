using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty ListWithFixedPrice(ListWithFixedPriceInput input)
        {
            CheckSenderNFTBalanceAndAllowance(input.Symbol, input.TokenId, input.Quantity);
            var listedNftInfo = new ListedNFTInfo
            {
                ListType = ListType.FixedPrice,
                Owner = Context.Sender,
                Price = input.Price,
                Quantity = input.Quantity,
                Symbol = input.Symbol,
                TokenId = input.TokenId
            };
            State.ListedNftInfoMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfo;
            Context.Fire(new ListedNFTInfoChanged
            {
                ListType = listedNftInfo.ListType,
                Owner = listedNftInfo.Owner,
                Price = listedNftInfo.Price,
                Quantity = listedNftInfo.Quantity,
                Symbol = listedNftInfo.Symbol,
                TokenId = listedNftInfo.TokenId
            });
            return new Empty();
        }
    }
}