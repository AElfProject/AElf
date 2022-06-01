using AElf.Contracts.NFTMinter.Managers;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        private IWhitelistManager GetWhitelistManager()
        {
            return new WhitelistManager(Context, State.WhitelistIdMap, State.WhitelistContract);
        }
    }
}