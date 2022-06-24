using System.Collections.Generic;
using AElf.Types;

namespace AElf.Contracts.NFTMarket.Services;

public interface IMakeOfferService
{
    void ValidateOffer(MakeOfferInput makeOfferInput);
    bool IsSenderInWhitelist(MakeOfferInput makeOfferInput,out Hash whitelistId);
    DealStatus GetDealStatus(MakeOfferInput makeOfferInput, out List<ListedNFTInfo> affordableNftInfoList);
}

public enum DealStatus
{
    NFTNotMined,
    NotDeal,
    DealWithOnePrice,
    DealWithMultiPrice,
}