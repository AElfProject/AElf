using System.Collections.Generic;

namespace AElf.Contracts.NFTMarket.Services;

public interface IDealService
{
    IEnumerable<DealResult> GetDealResultList(GetDealResultListInput input);
}

public record GetDealResultListInput(MakeOfferInput MakeOfferInput, ListedNFTInfoList ListedNftInfoList);

public record DealResult(string Symbol, long TokenId, long Quantity, string PurchaseSymbol, long PurchaseTokenId,
    long PurchaseAmount);
