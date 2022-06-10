using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist;

public partial class WhitelistContract
{
    public override WhitelistInfo GetWhitelist(Hash input)
    {
        var whitelist = AssertWhitelistInfo(input);
        return whitelist;
    }

    public override ExtraInfoList GetWhitelistDetail(Hash input)
    {
        var whitelistInfo = GetWhitelist(input);
        return ConvertToInfoList(whitelistInfo.ExtraInfoIdList);
    }

    public override WhitelistIdList GetWhitelistByProject(Hash input)
    {
        return State.WhitelistProjectMap[input];
    }

    public override TagInfo GetTagInfoByHash(Hash input)
    {
        Assert(State.TagInfoMap[input] != null, $"Not found tag.{input.ToHex()}");
        return State.TagInfoMap[input];
    }

    public override ExtraInfo GetExtraInfoByTag(GetExtraInfoByTagInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        var tagInfo = GetTagInfoByHash(input.TagInfoId);
        Assert(State.TagInfoIdAddressListMap[whitelist.WhitelistId][input.TagInfoId].Value.Count != 0,
            $"No address list under the current tag.{input.TagInfoId.ToHex()}");
        var addressList = State.TagInfoIdAddressListMap[whitelist.WhitelistId][input.TagInfoId];
        return new ExtraInfo
        {
            Info = tagInfo,
            AddressList = addressList
        };
    }

    public override HashList GetExtraInfoIdList(GetExtraInfoIdListInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        Assert(State.ManagerTagInfoMap[input.ProjectId][whitelist.WhitelistId] != null,
            $"ExtraInfo id list doesn't exist.{input.ProjectId.ToHex()}{input.WhitelistId.ToHex()}");
        var idList = State.ManagerTagInfoMap[input.ProjectId][whitelist.WhitelistId];
        Assert(idList.Value.Count != 0, $"No extraInfo id list.{input.ProjectId.ToHex()}{input.WhitelistId.ToHex()}");
        return idList;
    }

    public override TagInfo GetExtraInfoByAddress(GetExtraInfoByAddressInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        Assert(State.AddressTagInfoIdMap[whitelist.WhitelistId][input.Address] != null,
            $"No Match tagInfo according to the address.{input.WhitelistId.ToHex()}{input.Address}");
        var tagInfoId = State.AddressTagInfoIdMap[whitelist.WhitelistId][input.Address];
        var tagInfo = GetTagInfoByHash(tagInfoId);
        return tagInfo;
    }

    public override Hash GetTagIdByAddress(GetTagIdByAddressInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        Assert(State.AddressTagInfoIdMap[whitelist.WhitelistId][input.Address] != null,
            $"No Match tagInfo according to the address.{input.WhitelistId.ToHex()}{input.Address}");
        return State.AddressTagInfoIdMap[whitelist.WhitelistId][input.Address];
    }

    public override SubscribeWhitelistInfo GetSubscribeWhitelist(Hash input)
    {
        var subscribeInfo = AssertSubscribeWhitelistInfo(input);
        return subscribeInfo;
    }

    public override ConsumedList GetConsumedList(Hash input)
    {
        var subscribeInfo = GetSubscribeWhitelist(input);
        return State.ConsumedListMap[subscribeInfo.SubscribeId];
    }

    public override ExtraInfoIdList GetAvailableWhitelist(Hash input)
    {
        var subscribe = GetSubscribeWhitelist(input);
        var consumedList = State.ConsumedListMap[subscribe.SubscribeId];
        var whitelist = State.WhitelistInfoMap[subscribe.WhitelistId];
        if (consumedList.ExtraInfoIdList == null)
        {
            return whitelist.ExtraInfoIdList;
        }
        else
        {
            var consumedExtraList = consumedList.ExtraInfoIdList.Value;
            var whitelistExtra = whitelist.ExtraInfoIdList.Value;
            foreach (var extraInfoId in consumedExtraList)
            {
                var target = whitelistExtra.SingleOrDefault(e => e.Id == extraInfoId.Id);
                if (target == null) continue;
                var targetAddressList = target.AddressList;
                var available = targetAddressList.Value.Except(extraInfoId.AddressList.Value);
                target.AddressList = new AddressList() { Value = { available } };
            }

            return new ExtraInfoIdList() { Value = { whitelistExtra } };
        }
    }

    public override BoolValue GetAddressFromWhitelist(GetAddressFromWhitelistInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        var addressLists = whitelist.ExtraInfoIdList.Value.Select(e => e.AddressList).ToList();
        return addressLists.Any(addressList => addressList.Value.Contains(input.Address))
            ? new BoolValue { Value = true }
            : new BoolValue();
    }

    public override BoolValue GetExtraInfoFromWhitelist(GetExtraInfoFromWhitelistInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        var extraInfoId = whitelist.ExtraInfoIdList.Value.SingleOrDefault(i => i.Id == input.ExtraInfoId.Id);
        if (extraInfoId == null)
        {
            throw new AssertionException($"TagInfo does not exist.{input.ExtraInfoId.Id}");
        }

        var addressList = State.TagInfoIdAddressListMap[whitelist.WhitelistId][input.ExtraInfoId.Id] ??
                          new AddressList();
        return input.ExtraInfoId.AddressList.Value.Any(address => !addressList.Value.Contains(address))
            ? new BoolValue() { Value = false }
            : new BoolValue() { Value = true };
    }

    public override BoolValue GetManagerExistFromWhitelist(GetManagerExistFromWhitelistInput input)
    {
        var whitelistIdList = State.ManagerListMap[input.WhitelistId] ?? new AddressList();
        var ifExist = whitelistIdList.Value.Contains(input.Manager);
        return new BoolValue() { Value = ifExist };
    }

    public override BoolValue GetTagInfoFromWhitelist(GetTagInfoFromWhitelistInput input)
    {
        var whitelist = GetWhitelist(input.WhitelistId);
        MakeSureProjectCorrect(whitelist.WhitelistId, input.ProjectId);
        var tagId = whitelist.WhitelistId.CalculateExtraInfoId(whitelist.ProjectId, input.TagInfo.TagName);
        var tagIdList = State.ManagerTagInfoMap[whitelist.ProjectId][whitelist.WhitelistId] ?? new HashList();
        var ifExist = tagIdList.Value.Contains(tagId);
        return new BoolValue() { Value = ifExist };
    }


    public override BoolValue GetFromAvailableWhitelist(GetFromAvailableWhitelistInput input)
    {
        var subscribeInfo = GetSubscribeWhitelist(input.SubscribeId);
        var extraInfoIdList = GetAvailableWhitelist(input.SubscribeId);
        var extraInfoId = extraInfoIdList.Value.SingleOrDefault(i => i.Id == input.ExtraInfoId.Id);
        if (extraInfoId == null)
        {
            throw new AssertionException($"TagInfo does not exist.{input.ExtraInfoId.Id}");
        }
        else
        {
            var addressList = State.TagInfoIdAddressListMap[subscribeInfo.WhitelistId][input.ExtraInfoId.Id];
            return input.ExtraInfoId.AddressList.Value.Any(address => !addressList.Value.Contains(address))
                ? new BoolValue() { Value = false }
                : new BoolValue() { Value = true };
        }
    }

    public override WhitelistIdList GetWhitelistByManager(Address input)
    {
        var whitelistIdList = State.WhitelistIdMap[input];
        Assert(whitelistIdList != null && whitelistIdList.WhitelistId.Count != 0,
            $"No whitelist according to the manager.{input}");
        return State.WhitelistIdMap[input];
    }

    public override AddressList GetManagerList(Hash input)
    {
        AssertWhitelistInfo(input);
        return State.ManagerListMap[input];
    }

    public override AddressList GetSubscribeManagerList(Hash input)
    {
        AssertSubscribeWhitelistInfo(input);
        return State.SubscribeManagerListMap[input];
    }

    public override HashList GetSubscribeIdByManager(Address input)
    {
        var subscribeIdList = State.ManagerSubscribeIdListMap[input];
        Assert(subscribeIdList != null && subscribeIdList.Value.Count != 0,
            $"No subscribe id according to the manager.{input}");
        return subscribeIdList;
    }
}