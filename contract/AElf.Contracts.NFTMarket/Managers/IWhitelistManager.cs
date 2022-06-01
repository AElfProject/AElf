using AElf.Contracts.Whitelist;

namespace AElf.Contracts.NFTMinter.Managers
{
    internal interface IWhitelistManager
    {
        void CreateWhitelist(CreateWhitelistInput input);
        void AddExtraInfo(AddExtraInfoInput input);
        void AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input);
        void RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input);
    }
}