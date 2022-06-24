using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.NFTMarket.Services;

public class DealService : IDealService
{
    private readonly CSharpSmartContractContext _context;

    public DealService(CSharpSmartContractContext context)
    {
        _context = context;
    }

    public IEnumerable<DealResult> GetDealResultList(GetDealResultListInput input)
    {
        var dealResultList = new List<DealResult>();
        var needToDealQuantity = input.MakeOfferInput.Quantity;
        var currentIndex = 0;
        foreach (var listedNftInfo in input.ListedNftInfoList.Value.Where(i =>
                     i.Price.Symbol == input.MakeOfferInput.Price.Symbol && IsTimeOk(i)).OrderBy(i => i.Price.Amount))
        {
            if (listedNftInfo.Quantity >= needToDealQuantity)
            {
                var dealResult = new DealResult
                {
                    Symbol = input.MakeOfferInput.Symbol,
                    TokenId = input.MakeOfferInput.TokenId,
                    Quantity = needToDealQuantity,
                    PurchaseSymbol = input.MakeOfferInput.Price.Symbol,
                    PurchaseAmount = listedNftInfo.Price.Amount,
                    Duration = listedNftInfo.Duration,
                    Index = currentIndex
                };
                // Fulfill demands.
                dealResultList.Add(dealResult);
                needToDealQuantity = 0;
            }
            else
            {
                var dealResult = new DealResult
                {
                    Symbol = input.MakeOfferInput.Symbol,
                    TokenId = input.MakeOfferInput.TokenId,
                    Quantity = needToDealQuantity,
                    PurchaseSymbol = input.MakeOfferInput.Price.Symbol,
                    PurchaseAmount = listedNftInfo.Price.Amount,
                    Duration = listedNftInfo.Duration,
                    Index = currentIndex
                };
                dealResultList.Add(dealResult);
                needToDealQuantity = needToDealQuantity.Sub(listedNftInfo.Quantity);
            }

            if (needToDealQuantity == 0)
            {
                break;
            }

            currentIndex = currentIndex.Add(1);
        }

        return dealResultList;
    }

    private bool IsTimeOk(ListedNFTInfo listedNftInfo)
    {
        return _context.CurrentBlockTime >= listedNftInfo.Duration.StartTime && _context.CurrentBlockTime >= listedNftInfo.Duration.PublicTime;
    }
}