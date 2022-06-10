using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Whitelist;

public partial class WhitelistContract
{
    private Hash CalculateWhitelistHash(Address address, Hash projectId)
    {
        return Context.GenerateId(Context.Self,
            ByteArrayHelper.ConcatArrays(address.ToByteArray(), projectId.ToByteArray()));
    }

    private Hash CalculateSubscribeWhitelistHash(Address address, Hash projectId, Hash whitelistId)
    {
        return HashHelper.ComputeFrom($"{address}{projectId}{whitelistId}");
    }

    private Hash CalculateCloneWhitelistHash(Address address, Hash whitelistId)
    {
        return HashHelper.ComputeFrom($"{address}{whitelistId}");
    }

    private WhitelistInfo AssertWhitelistInfo(Hash whitelistId)
    {
        var whitelistInfo = State.WhitelistInfoMap[whitelistId];
        Assert(whitelistInfo != null, $"Whitelist not found.{whitelistId.ToHex()}");
        return whitelistInfo;
    }

    private void AssertWhitelistIsAvailable(Hash whitelistId)
    {
        var whitelistInfo = State.WhitelistInfoMap[whitelistId];
        Assert(whitelistInfo.IsAvailable, $"Whitelist is not available.{whitelistId.ToHex()}");
    }

    private WhitelistInfo AssertWhitelistManager(Hash whitelistId)
    {
        var whitelistInfo = GetWhitelist(whitelistId);
        Assert(whitelistInfo.Manager.Value.Contains(Context.Sender),
            $"{Context.Sender} is not the manager of the whitelist.");
        return whitelistInfo;
    }

    private WhitelistInfo AssertWhitelistCreator(Hash whitelistId)
    {
        var whitelistInfo = GetWhitelist(whitelistId);
        Assert(whitelistInfo.Creator == Context.Sender, $"{Context.Sender}No permission.");
        return whitelistInfo;
    }

    private void MakeSureProjectCorrect(Hash whitelistId, Hash projectId)
    {
        Assert(State.WhitelistProjectMap[projectId] != null, $"Incorrect project id.{projectId}");
        Assert(State.WhitelistProjectMap[projectId].WhitelistId.Contains(whitelistId),
            $"Incorrect whitelist id.{whitelistId}");
    }

    private SubscribeWhitelistInfo AssertSubscribeWhitelistInfo(Hash subscribeId)
    {
        var subscribeInfo = State.SubscribeWhitelistInfoMap[subscribeId];
        Assert(subscribeInfo != null, $"Subscribe info not found.{subscribeId.ToHex()}");
        return subscribeInfo;
    }

    private void AssertSubscribeManager(Hash subscribeId, Address address)
    {
        var managerList = State.SubscribeManagerListMap[subscribeId];
        if (!managerList.Value.Contains(address))
        {
            throw new AssertionException($"No permission.{address}");
        }
    }

    private void AssertSubscriber(Hash subscribeId)
    {
        var subscribeWhitelistInfo = State.SubscribeWhitelistInfoMap[subscribeId];
        Assert(subscribeWhitelistInfo.Subscriber == Context.Sender, $"{Context.Sender}No permission.");
    }

    private ExtraInfoId AssertExtraInfoDuplicate(Hash whitelistId, ExtraInfoId id)
    {
        var whitelist = State.WhitelistInfoMap[whitelistId];
        foreach (var addressList in whitelist.ExtraInfoIdList.Value.Select(i => i.AddressList))
        {
            foreach (var address in id.AddressList.Value)
            {
                if (addressList.Value.Contains(address))
                {
                    throw new AssertionException($"Duplicate address.{address}");
                }
            }
        }

        return id;
    }

    private ExtraInfoId AssertExtraInfoIsNotExist(Hash subscribeId, ExtraInfoId infoId)
    {
        var whitelist = GetAvailableWhitelist(subscribeId);
        var extraInfo = whitelist.Value.SingleOrDefault(e => e.Id == infoId.Id);
        if (extraInfo == null)
        {
            throw new AssertionException($"Incorrect ExtraInfo id.{infoId}");
        }

        if (infoId.AddressList.Value.Select(address => extraInfo.AddressList.Value.Contains(address))
            .Any(ifExist => !ifExist))
        {
            throw new AssertionException($"ExtraInfo doesn't exist in the available whitelist.{infoId}");
        }

        return infoId;
    }

    private ExtraInfo ConvertToInfo(ExtraInfoId extraInfoId)
    {
        var extraInfo = State.TagInfoMap[extraInfoId.Id];
        return new ExtraInfo()
        {
            AddressList = extraInfoId.AddressList,
            Info = new TagInfo()
            {
                TagName = extraInfo.TagName,
                Info = extraInfo.Info
            }
        };
    }

    private ExtraInfoList ConvertToInfoList(ExtraInfoIdList extraInfoIdList)
    {
        var extraInfo = extraInfoIdList.Value.Select(e =>
        {
            if (e.Id != null)
            {
                var infoId = e.Id;
                var info = State.TagInfoMap[infoId];
                return new ExtraInfo()
                {
                    AddressList = e.AddressList,
                    Info = new TagInfo()
                    {
                        TagName = info.TagName,
                        Info = info.Info
                    }
                };
            }

            return new ExtraInfo()
            {
                AddressList = e.AddressList,
                Info = null
            };
        }).ToList();
        return new ExtraInfoList() { Value = { extraInfo } };
    }

    /// <summary>
    /// Create TagInfo when creating whitelist with tagInfo.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="projectId"></param>
    /// <param name="whitelistId"></param>
    /// <returns></returns>
    /// <exception cref="AssertionException"></exception>
    private Hash CreateTagInfo(TagInfo info, Hash projectId, Hash whitelistId)
    {
        if (info == null)
        {
            throw new AssertionException("TagInfo is null.");
        }

        var id = whitelistId.CalculateExtraInfoId(projectId, info.TagName);
        if (State.TagInfoMap[id] != null)
        {
            throw new AssertionException($"TagInfo already exist.{info}");
        }

        State.TagInfoMap[id] = info;
        Context.Fire(new TagInfoAdded()
        {
            WhitelistId = whitelistId,
            ProjectId = projectId,
            TagInfoId = id,
            TagInfo = State.TagInfoMap[id]
        });
        return id;
    }

    private AddressList SetManagerList(Hash whitelistId, Address creator, AddressList input)
    {
        var managerList = input != null ? input.Value.Distinct().ToList() : new List<Address>();
        if (!managerList.Contains(creator ?? Context.Sender))
        {
            managerList.Add(creator ?? Context.Sender);
        }

        State.ManagerListMap[whitelistId] = new AddressList() { Value = { managerList } };
        return State.ManagerListMap[whitelistId];
    }

    private AddressList SetSubscribeManagerList(Hash subscribeId, Address creator, AddressList input)
    {
        var managerList = input != null ? input.Value.Distinct().ToList() : new List<Address>();
        if (!managerList.Contains(creator ?? Context.Sender))
        {
            managerList.Add(creator ?? Context.Sender);
        }

        State.SubscribeManagerListMap[subscribeId] = new AddressList() { Value = { managerList } };
        return State.SubscribeManagerListMap[subscribeId];
    }

    private void SetWhitelistIdManager(Hash whitelistId, AddressList managerList)
    {
        foreach (var manager in managerList.Value)
        {
            var whitelistIdList = State.WhitelistIdMap[manager] ?? new WhitelistIdList();
            whitelistIdList.WhitelistId.Add(whitelistId);
            State.WhitelistIdMap[manager] = whitelistIdList;
        }
    }

    private void SetManagerListToWhitelist(Hash whitelistId, AddressList addressList)
    {
        var whitelistInfo = GetWhitelist(whitelistId);
        whitelistInfo.Manager.Value.Add(addressList.Value);

    }

    private void SetSubscribeIdManager(Hash subscribeId, AddressList managerList)
    {
        foreach (var manager in managerList.Value)
        {
            var subscribeIdList = State.ManagerSubscribeIdListMap[manager] ?? new HashList();
            subscribeIdList.Value.Add(subscribeId);
            State.ManagerSubscribeIdListMap[manager] = subscribeIdList;
        }
    }

    private void RemoveWhitelistIdManager(Hash whitelistId, AddressList managerList)
    {
        foreach (var manager in managerList.Value)
        {
            var whitelistIdList = State.WhitelistIdMap[manager];
            if (whitelistIdList == null) continue;
            whitelistIdList.WhitelistId.Remove(whitelistId);
            State.WhitelistIdMap[manager] = whitelistIdList;
        }
    }

    private void RemoveManagerListFromWhitelist(Hash whitelistId, AddressList managerList)
    {
        var whitelistInfo = GetWhitelist(whitelistId);
        foreach (var manager in managerList.Value)
        {
            Assert(whitelistInfo.Manager.Value.Contains(manager), $"Manager is not exist.{manager}");
            whitelistInfo.Manager.Value.Remove(manager);
        }
    }

    private void RemoveSubscribeIdManager(Hash subscribeId, AddressList managerList)
    {
        foreach (var manager in managerList.Value)
        {
            var subscribeIdList = State.ManagerSubscribeIdListMap[manager];
            if (subscribeIdList == null) continue;
            subscribeIdList.Value.Remove(subscribeId);
            State.ManagerSubscribeIdListMap[manager] = subscribeIdList;
        }
    }
}