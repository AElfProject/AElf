using AElf.Contracts.NFTMarket.Managers;
using AElf.Contracts.NFTMarket.Services;

namespace AElf.Contracts.NFTMarket;

public partial class NFTMarketContract
{
    private WhitelistManager GetWhitelistManager()
    {
        return new WhitelistManager(Context, State.WhitelistIdMap, State.WhitelistContract);
    }

    private MakeOfferService GetMakeOfferService(WhitelistManager whitelistManager = null)
    {
        return new MakeOfferService(State.NFTContract, State.WhitelistIdMap, State.ListedNFTInfoListMap,
            whitelistManager ?? GetWhitelistManager(), Context);
    }

    private DealService GetDealService()
    {
        return new DealService(Context);
    }
}