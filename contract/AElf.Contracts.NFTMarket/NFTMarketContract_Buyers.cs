using AElf.CSharp.Core;
using AElf.Types;
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
                OnlyMakeOffer(input);
                return new Empty();
            }

            Assert(listedNftInfo.Price.Symbol == input.Price.Symbol, "Symbol not match.");

            Assert(listedNftInfo.Quantity >= input.Quantity, "Quantity exceeded.");

            if (listedNftInfo.ListType == ListType.FixedPrice)
            {
                if (input.Price.Amount >= listedNftInfo.Price.Amount)
                {
                    TryDealWithFixedPrice(input, listedNftInfo.Price.Amount);
                }
                else
                {
                    OnlyMakeOffer(input);
                }
            }

            return new Empty();
        }

        /// <summary>
        /// Sender is buyer.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="price"></param>
        private void TryDealWithFixedPrice(MakeOfferInput input, long price)
        {
            var amount = price.Mul(input.Quantity);
            PerformDeal(new PerformDealInput
            {
                NFTFrom = input.Owner,
                NFTTo = Context.Sender,
                NFTSymbol = input.Symbol,
                NFTTokenId = input.TokenId,
                NFTQuantity = input.Quantity,
                PurchaseSymbol = input.Price.Symbol,
                PurchaseAmount = amount
            });
        }

        private void OnlyMakeOffer(MakeOfferInput input)
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

        private void PerformDeal(PerformDealInput performDealInput)
        {
            var serviceFee = performDealInput.PurchaseAmount.Mul(State.ServiceFeeRate.Value).Div(ServiceFeeDenominator);
            var actualAmount = performDealInput.PurchaseAmount.Sub(serviceFee);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = performDealInput.NFTTo,
                To = performDealInput.NFTFrom,
                Symbol = performDealInput.PurchaseSymbol,
                Amount = actualAmount
            });
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = performDealInput.NFTTo,
                To = State.ServiceFeeReceiver.Value,
                Symbol = performDealInput.PurchaseSymbol,
                Amount = serviceFee
            });
            State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
            {
                From = performDealInput.NFTFrom,
                To = performDealInput.NFTTo,
                Symbol = performDealInput.NFTSymbol,
                TokenId = performDealInput.NFTTokenId,
                Amount = performDealInput.NFTQuantity
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
            public long PurchaseAmount { get; set; }
        }
    }
}