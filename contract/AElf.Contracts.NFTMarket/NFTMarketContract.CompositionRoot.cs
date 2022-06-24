using AElf.Contracts.NFTMarket.Managers;
using AElf.Contracts.NFTMarket.Services;

namespace AElf.Contracts.NFTMarket;

public partial class NFTMarketContract
{
    private IWhitelistManager GetWhitelistManager()
    {
        return new WhitelistManager(Context, State.WhitelistIdMap, State.WhitelistContract);
    }

    private IMakeOfferService GetMakeOfferService(IWhitelistManager? whitelistManager = null)
    {
        return new MakeOfferService(State.NFTContract, State.WhitelistIdMap, State.ListedNFTInfoListMap,
            whitelistManager ?? GetWhitelistManager(), Context);
    }

    private IDealService GetDealService()
    {
        return new DealService(Context);
    }
}