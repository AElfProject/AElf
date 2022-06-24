using System.Collections.Generic;

namespace AElf.Contracts.NFTMarket.Services;

public interface IDealService
{
    IEnumerable<DealResult> GetDealResultList(GetDealResultListInput input);
}

public class GetDealResultListInput
{
    internal MakeOfferInput MakeOfferInput { get; set; }
    internal ListedNFTInfoList ListedNftInfoList{ get; set; }
}


public class DealResult
{
    internal string Symbol { get; set; }
    internal long TokenId{ get; set; }
    internal long Quantity{ get; set; }
    internal string PurchaseSymbol{ get; set; }
    internal long PurchaseTokenId{ get; set; }
    internal long PurchaseAmount{ get; set; }
    internal ListDuration Duration { get; set; }
    internal int Index { get; set; }
}

