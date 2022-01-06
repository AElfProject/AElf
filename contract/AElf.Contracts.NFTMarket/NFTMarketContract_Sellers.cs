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
                    DurationMinutes = int.MaxValue
                };
            }
            else
            {
                if (duration.StartTime == null || duration.StartTime > Context.CurrentBlockTime)
                {
                    duration.StartTime = Context.CurrentBlockTime;
                }

                if (duration.DurationMinutes == 0)
                {
                    duration.DurationMinutes = int.MaxValue;
                }
            }

            var listedNftInfo = new ListedNFTInfo
            {
                ListType = ListType.FixedPrice,
                Owner = Context.Sender,
                Price = input.Price,
                Quantity = input.Quantity,
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                Duration = duration
            };
            State.ListedNftInfoMap[input.Symbol][input.TokenId][Context.Sender] = listedNftInfo;
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
    }
}