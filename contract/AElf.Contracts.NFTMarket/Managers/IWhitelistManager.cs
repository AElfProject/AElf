using AElf.Contracts.Whitelist;
using AElf.Types;

namespace AElf.Contracts.NFTMarket.Managers
{
    internal interface IWhitelistManager
    {
        void CreateWhitelist(CreateWhitelistInput input);
        void AddExtraInfo(AddExtraInfoInput input);
        void AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input);
        void RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input);
        bool IsAddressInWhitelist(Address address, Hash whitelistId);
        TagInfo GetExtraInfoByAddress(GetExtraInfoByAddressInput input);
    }
}