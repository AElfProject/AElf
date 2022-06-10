using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist;

public partial class WhitelistContract
{
    public override Hash SubscribeWhitelist(SubscribeWhitelistInput input)
    {
        var whitelistInfo = AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(whitelistInfo.WhitelistId);
        var subscribeId = CalculateSubscribeWhitelistHash(Context.Sender, input.ProjectId, input.WhitelistId);
        Assert(State.SubscribeWhitelistInfoMap[subscribeId] == null, "Subscribe info already exist.");
        var managerList = SetManagerList(subscribeId, input.Subscriber, input.ManagerList);
        var subscribeWhiteListInfo = new SubscribeWhitelistInfo
        {
            SubscribeId = subscribeId,
            ProjectId = input.ProjectId,
            WhitelistId = whitelistInfo.WhitelistId,
            Subscriber = input.Subscriber ?? Context.Sender,
            ManagerList = input.ManagerList
        };
        State.SubscribeWhitelistInfoMap[subscribeId] = subscribeWhiteListInfo;
        State.SubscribeManagerListMap[subscribeId] =
            SetSubscribeManagerList(subscribeId, input.Subscriber, input.ManagerList);
        State.ConsumedListMap[subscribeId] = new ConsumedList();
        SetSubscribeIdManager(subscribeId, managerList);
        Context.Fire(new WhitelistSubscribed
        {
            SubscribeId = subscribeId,
            ProjectId = subscribeWhiteListInfo.ProjectId,
            WhitelistId = subscribeWhiteListInfo.WhitelistId,
            Subscriber = input.Subscriber ?? Context.Sender,
            ManagerList = input.ManagerList
        });
        return subscribeId;
    }

    public override Empty UnsubscribeWhitelist(Hash input)
    {
        var subscribeInfo = AssertSubscribeWhitelistInfo(input);
        AssertSubscribeManager(subscribeInfo.SubscribeId, Context.Sender);
        State.ConsumedListMap.Remove(input);
        Context.Fire(new WhitelistUnsubscribed()
        {
            SubscribeId = subscribeInfo.SubscribeId,
            ProjectId = subscribeInfo.ProjectId,
            WhitelistId = subscribeInfo.WhitelistId
        });
        return new Empty();
    }

    public override Empty ConsumeWhitelist(ConsumeWhitelistInput input)
    {
        var subscribeInfo = AssertSubscribeWhitelistInfo(input.SubscribeId);
        AssertSubscribeManager(subscribeInfo.SubscribeId, Context.Sender);
        var extraInfoId = AssertExtraInfoIsNotExist(subscribeInfo.SubscribeId, input.ExtraInfoId);
        if (State.ConsumedListMap[subscribeInfo.SubscribeId].ExtraInfoIdList != null)
        {
            var consumedList = GetConsumedList(subscribeInfo.SubscribeId);
            var targetConsume = consumedList.ExtraInfoIdList.Value.SingleOrDefault(e => e.Id == extraInfoId.Id);
            if (targetConsume != null)
            {
                targetConsume.AddressList.Value.AddRange(extraInfoId.AddressList.Value);
            }
            else
            {
                consumedList.ExtraInfoIdList.Value.Add(extraInfoId);
            }

            State.ConsumedListMap[subscribeInfo.SubscribeId] = consumedList;
            Context.Fire(new ConsumedListAdded
            {
                SubscribeId = consumedList.SubscribeId,
                WhitelistId = subscribeInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList { Value = { extraInfoId } }
            });
        }
        else
        {
            var addressInfoList = new ExtraInfoIdList();
            addressInfoList.Value.Add(extraInfoId);
            var consumedList = new ConsumedList
            {
                SubscribeId = subscribeInfo.SubscribeId,
                WhitelistId = subscribeInfo.WhitelistId,
                ExtraInfoIdList = addressInfoList
            };
            State.ConsumedListMap[subscribeInfo.SubscribeId] = consumedList;
            Context.Fire(new ConsumedListAdded
            {
                SubscribeId = consumedList.SubscribeId,
                WhitelistId = subscribeInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList { Value = { extraInfoId } }
            });
        }

        return new Empty();
    }

    public override Empty AddSubscribeManagers(AddSubscribeManagersInput input)
    {
        AssertSubscriber(input.SubscribeId);
        var managerList = State.SubscribeManagerListMap[input.SubscribeId];
        var addedManager = new AddressList();
        foreach (var manager in input.ManagerList.Value)
        {
            if (!managerList.Value.Contains(manager))
            {
                managerList.Value.Add(manager);
                addedManager.Value.Add(manager);
            }
            else
            {
                throw new AssertionException($"Managers already exists.{manager}");
            }
        }

        State.SubscribeManagerListMap[input.SubscribeId] = managerList;
        SetSubscribeIdManager(input.SubscribeId, addedManager);

        Context.Fire(new SubscribeManagerAdded()
        {
            SubscribeId = input.SubscribeId,
            ManagerList = addedManager
        });
        return new Empty();
    }

    public override Empty RemoveSubscribeManagers(RemoveSubscribeManagersInput input)
    {
        AssertSubscriber(input.SubscribeId);
        var managerList = State.SubscribeManagerListMap[input.SubscribeId];
        var removedList = new AddressList();
        foreach (var manager in input.ManagerList.Value)
        {
            if (managerList.Value.Contains(manager))
            {
                managerList.Value.Remove(manager);
                removedList.Value.Add(manager);
            }
            else
            {
                throw new AssertionException($"Managers doesn't exists.{manager}");
            }
        }

        State.SubscribeManagerListMap[input.SubscribeId] = managerList;
        RemoveSubscribeIdManager(input.SubscribeId, removedList);
        Context.Fire(new SubscribeManagerRemoved
        {
            SubscribeId = input.SubscribeId,
            ManagerList = removedList
        });
        return new Empty();
    }

    public override Hash CloneWhitelist(CloneWhitelistInput input)
    {
        var whiteListInfo = AssertWhitelistInfo(input.WhitelistId).Clone();
        AssertWhitelistIsAvailable(whiteListInfo.WhitelistId);
        Assert(whiteListInfo.IsCloneable, $"Whitelist is not allowed to be cloned.{whiteListInfo.WhitelistId.ToHex()}");

        var cloneWhiteListId = CalculateCloneWhitelistHash(Context.Sender, input.WhitelistId);
        Assert(State.WhitelistInfoMap[cloneWhiteListId] == null, "WhiteList has already been cloned.");

        var managerList = SetManagerList(cloneWhiteListId, input.Creator, input.ManagerList);
        var whitelistClone = new WhitelistInfo()
        {
            WhitelistId = cloneWhiteListId,
            ExtraInfoIdList = whiteListInfo.ExtraInfoIdList,
            Creator = input.Creator ?? Context.Sender,
            IsAvailable = whiteListInfo.IsAvailable,
            IsCloneable = whiteListInfo.IsCloneable,
            Remark = whiteListInfo.Remark,
            CloneFrom = whiteListInfo.WhitelistId,
            Manager = managerList,
            ProjectId = input.ProjectId,
            StrategyType = whiteListInfo.StrategyType
        };
        State.WhitelistInfoMap[cloneWhiteListId] = whitelistClone;

        SetWhitelistIdManager(cloneWhiteListId, managerList);

        Context.Fire(new WhitelistCreated()
        {
            WhitelistId = whitelistClone.WhitelistId,
            ExtraInfoIdList = whitelistClone.ExtraInfoIdList,
            Creator = input.Creator ?? Context.Sender,
            IsAvailable = whitelistClone.IsAvailable,
            IsCloneable = whitelistClone.IsCloneable,
            Remark = whitelistClone.Remark,
            CloneFrom = whiteListInfo.WhitelistId,
            Manager = managerList,
            ProjectId = input.ProjectId,
            StrategyType = whitelistClone.StrategyType
        });
        return cloneWhiteListId;
    }
}