using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;

namespace AElf.Contracts.NFTMarket.Services;

public class DealService : IDealService
{
    public IEnumerable<DealResult> GetDealResultList(GetDealResultListInput input)
    {
        var dealResultList = new List<DealResult>();
        var needToDealQuantity = input.MakeOfferInput.Quantity;
        foreach (var listedNftInfo in input.ListedNftInfoList.Value.Where(i =>
                     i.Symbol == input.MakeOfferInput.Price.Symbol).OrderBy(i => i.Price.Amount))
        {
            if (listedNftInfo.Quantity >= needToDealQuantity)
            {
                // Fulfill demands.
                dealResultList.Add(new DealResult(
                    input.MakeOfferInput.Symbol,
                    input.MakeOfferInput.TokenId,
                    needToDealQuantity,
                    input.MakeOfferInput.Price.Symbol,
                    input.MakeOfferInput.Price.TokenId,
                    listedNftInfo.Price.Amount
                ));
                needToDealQuantity = 0;
            }
            else
            {
                dealResultList.Add(new DealResult(
                    input.MakeOfferInput.Symbol,
                    input.MakeOfferInput.TokenId,
                    needToDealQuantity,
                    input.MakeOfferInput.Price.Symbol,
                    input.MakeOfferInput.Price.TokenId,
                    listedNftInfo.Price.Amount
                ));
                needToDealQuantity = needToDealQuantity.Sub(listedNftInfo.Quantity);
            }

            if (needToDealQuantity == 0)
            {
                break;
            }
        }

        return dealResultList;
    }
}