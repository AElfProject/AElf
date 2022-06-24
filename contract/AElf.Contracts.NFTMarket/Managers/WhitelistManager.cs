using AElf.Contracts.NFTMarket.Helpers;
using AElf.Contracts.Whitelist;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NFTMarket.Managers;

internal class WhitelistManager : IWhitelistManager
{
    private readonly CSharpSmartContractContext _context;
    private readonly MappedState<Hash, Hash> _whitelistIdMap;
    private readonly WhitelistContractContainer.WhitelistContractReferenceState _whitelistContract;

    public WhitelistManager(CSharpSmartContractContext context,
        MappedState<Hash, Hash> whitelistIdMap,
        WhitelistContractContainer.WhitelistContractReferenceState whitelistContract)
    {
        _context = context;
        _whitelistIdMap = whitelistIdMap;
        _whitelistContract = whitelistContract;
    }

    public void CreateWhitelist(CreateWhitelistInput input)
    {
        _whitelistContract.CreateWhitelist.Send(input);
    }

    public void AddExtraInfo(AddExtraInfoInput input)
    {
        _whitelistContract.AddExtraInfo.Send(input);
    }

    public void AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
    {
        _whitelistContract.AddAddressInfoListToWhitelist.Send(input);
    }

    public void RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
    {
        _whitelistContract.RemoveAddressInfoListFromWhitelist.Send(input);
    }

    public bool IsAddressInWhitelist(Address address, Hash whitelistId)
    {
        if (whitelistId == null)
        {
            return false;
        }

        return _whitelistContract.GetAddressFromWhitelist.Call(new GetAddressFromWhitelistInput
        {
            Address = address,
            WhitelistId = whitelistId
        }).Value;
    }

    public Price GetExtraInfoByAddress(Hash whitelistId)
    {
        var tagInfo = _whitelistContract.GetExtraInfoByAddress.Call(new GetExtraInfoByAddressInput
        {
            WhitelistId = whitelistId,
            Address = _context.Sender
        });
        return WhitelistHelper.DeserializedInfo(tagInfo);
    }
}