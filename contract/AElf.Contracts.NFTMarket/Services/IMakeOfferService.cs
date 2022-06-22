using System.Collections.Generic;

namespace AElf.Contracts.NFTMarket.Services;

public interface IMakeOfferService
{
    void ValidateOffer(MakeOfferInput makeOfferInput);
    bool IsSenderInWhitelist(MakeOfferInput makeOfferInput);
    OfferStatus GetDealStatus(MakeOfferInput makeOfferInput, out List<ListedNFTInfo> affordableNftInfoList);
}

public enum OfferStatus
{
    NFTNotMined,
    NotDeal,
    DealWithOnePrice,
    DealWithMultiPrice,
}