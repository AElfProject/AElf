using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty MakeOffer(MakeOfferInput input)
        {
            Assert(Context.Sender != input.Owner, "Invalid owner.");
            var listedNftInfo = State.ListedNftInfoMap[input.Symbol][input.TokenId][input.Owner];

            if (listedNftInfo == null)
            {
                // NFT not listed by the owner.
                JustMakeOffer(input);
                return new Empty();
            }
            
            Assert(listedNftInfo.Price.Symbol == input.Price.Symbol, "Symbol not match.");

            Assert(listedNftInfo.Quantity >= input.Quantity, "Quantity exceeded.");

            if (listedNftInfo.ListType == ListType.FixedPrice)
            {
                if (input.Price.Amount >= listedNftInfo.Price.Amount)
                {
                    TryDealWithFixedPrice(input);
                }
                else
                {
                    JustMakeOffer(input);
                }
            }

            return new Empty();
        }

        private void TryDealWithFixedPrice(MakeOfferInput input)
        {
            var amount = input.Price.Amount.Mul(input.Quantity);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = input.Owner,
                Symbol = input.Price.Symbol,
                Amount = amount
            });
            State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
            {
                From = input.Owner,
                To = Context.Sender,
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Amount = input.Quantity
            });
        }

        private void JustMakeOffer(MakeOfferInput input)
        {
            var offerList = State.OfferListMap[input.Symbol][input.TokenId] ?? new OfferList();
            offerList.Value.Add(new Offer
            {
                From = Context.Sender,
                Price = input.Price,
                ExpireTime = input.ExpireTime
            });
            State.OfferListMap[input.Symbol][input.TokenId] = offerList;
        }
    }
}