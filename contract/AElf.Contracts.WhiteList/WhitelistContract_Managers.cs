using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist;

public partial class WhitelistContract
{
    public override Hash CreateWhitelist(CreateWhitelistInput input)
    {
        var whitelistHash = CalculateWhitelistHash(Context.Sender, input.ProjectId);

        Assert(State.WhitelistInfoMap[whitelistHash] == null, $"Whitelist already exists.{whitelistHash.ToHex()}");

        var managerList = SetManagerList(whitelistHash, input.Creator, input.ManagerList);

        var whitelistInfo = new WhitelistInfo
        {
            WhitelistId = whitelistHash,
            ProjectId = input.ProjectId,
            ExtraInfoIdList = null,
            Creator = input.Creator ?? Context.Sender,
            IsAvailable = true,
            IsCloneable = input.IsCloneable,
            Remark = input.Remark,
            Manager = managerList,
            StrategyType = input.StrategyType
        };
        Context.Fire(new WhitelistCreated
        {
            WhitelistId = whitelistHash,
            ProjectId = whitelistInfo.ProjectId,
            ExtraInfoIdList = whitelistInfo.ExtraInfoIdList,
            Creator = input.Creator ?? Context.Sender,
            IsCloneable = whitelistInfo.IsCloneable,
            IsAvailable = whitelistInfo.IsAvailable,
            Remark = whitelistInfo.Remark,
            Manager = whitelistInfo.Manager,
            StrategyType = whitelistInfo.StrategyType
        });
        var alreadyExistsAddressList = new List<Address>();
        //Remove duplicate addresses.
        var extraInfoList = input.ExtraInfoList.Value;
        if (input.StrategyType == StrategyType.Basic)
        {
            var addressList = new AddressList();
            foreach (var extraInfo in extraInfoList)
            {
                foreach (var address in extraInfo.AddressList.Value)
                {
                    if (addressList.Value.Contains(address))
                    {
                        throw new AssertionException($"Duplicate address: ${address}.");
                    }

                    addressList.Value.Add(address);
                }
            }

            var extraInfoIdList = new ExtraInfoIdList
            {
                Value =
                {
                    new ExtraInfoId
                    {
                        AddressList = addressList
                    }
                }
            };

            whitelistInfo.ExtraInfoIdList = extraInfoIdList;
            Context.Fire(new WhitelistAddressInfoAdded()
            {
                WhitelistId = whitelistHash,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList
            });
        }
        else
        {
            var extraInfoIdList = extraInfoList.Select(e =>
            {
                var id = CreateTagInfo(e.Info, input.ProjectId, whitelistHash);
                //Set tagInfo list according to the owner and projectId.
                var idList = State.ManagerTagInfoMap[input.ProjectId][whitelistHash] ??
                             new HashList();
                idList.Value.Add(id);
                State.ManagerTagInfoMap[input.ProjectId][whitelistHash] = idList;
                //Set address list according to the tagInfoId.
                var addressList = State.TagInfoIdAddressListMap[whitelistHash][id] ?? new AddressList();
                addressList.Value.AddRange(e.AddressList.Value);
                State.TagInfoIdAddressListMap[whitelistHash][id] = addressList;
                //Map address and tagInfoId.
                foreach (var address in e.AddressList.Value)
                {
                    State.AddressTagInfoIdMap[whitelistHash][address] = id;
                    if (alreadyExistsAddressList.Contains(address))
                    {
                        throw new AssertionException($"Duplicate address: ${address}.");
                    }

                    alreadyExistsAddressList.Add(address);
                }

                return new ExtraInfoId
                {
                    AddressList = e.AddressList,
                    Id = id
                };
            }).ToList();
            whitelistInfo.ExtraInfoIdList = new ExtraInfoIdList() { Value = { extraInfoIdList } };
            Context.Fire(new WhitelistAddressInfoAdded()
            {
                WhitelistId = whitelistHash,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList
            });
        }

        State.WhitelistInfoMap[whitelistHash] = whitelistInfo;
        SetWhitelistIdManager(whitelistHash, managerList);
        var projectWhitelist = State.WhitelistProjectMap[input.ProjectId] ?? new WhitelistIdList();
        projectWhitelist.WhitelistId.Add(whitelistHash);
        State.WhitelistProjectMap[input.ProjectId] = projectWhitelist;
        State.ProjectWhitelistIdMap[whitelistHash] = whitelistInfo.ProjectId;

        return whitelistHash;
    }

    public override Hash AddExtraInfo(AddExtraInfoInput input)
    {
        if (input == null)
        {
            throw new AssertionException("Extra info is null");
        }

        AssertWhitelistInfo(input.WhitelistId);
        MakeSureProjectCorrect(input.WhitelistId, input.ProjectId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelistInfo = AssertWhitelistManager(input.WhitelistId);

        var tagInfoId = whitelistInfo.WhitelistId.CalculateExtraInfoId(whitelistInfo.ProjectId, input.TagInfo.TagName);
        Assert(State.TagInfoMap[tagInfoId] == null, $"The tag Info {input.TagInfo.TagName} already exists.");
        State.TagInfoMap[tagInfoId] = new TagInfo()
        {
            TagName = input.TagInfo.TagName,
            Info = input.TagInfo.Info
        };
        var idList = State.ManagerTagInfoMap[input.ProjectId][input.WhitelistId] ?? new HashList();
        idList.Value.Add(tagInfoId);
        State.ManagerTagInfoMap[input.ProjectId][input.WhitelistId] = idList;
        State.TagInfoIdAddressListMap[input.WhitelistId][tagInfoId] = new AddressList();
        Context.Fire(new TagInfoAdded()
        {
            ProjectId = input.ProjectId,
            WhitelistId = input.WhitelistId,
            TagInfoId = tagInfoId,
            TagInfo = new TagInfo()
            {
                TagName = State.TagInfoMap[tagInfoId].TagName,
                Info = State.TagInfoMap[tagInfoId].Info
            }
        });

        //Add tagInfo with address list.
        if (input.AddressList == null) return tagInfoId;
        var extraInfoIdList = new ExtraInfoIdList()
        {
            Value =
            {
                new ExtraInfoId
                {
                    AddressList = input.AddressList,
                    Id = tagInfoId
                }
            }
        };
        AddAddressInfoListToWhitelist(new AddAddressInfoListToWhitelistInput()
        {
            WhitelistId = input.WhitelistId,
            ExtraInfoIdList = extraInfoIdList
        });
        return tagInfoId;
    }

    public override Empty RemoveTagInfo(RemoveTagInfoInput input)
    {
        if (input == null)
        {
            throw new AssertionException("Tag info is null");
        }

        MakeSureProjectCorrect(input.WhitelistId, input.ProjectId);
        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelist = AssertWhitelistManager(input.WhitelistId);

        Assert(State.ManagerTagInfoMap[input.ProjectId][input.WhitelistId].Value.Contains(input.TagId),
            $"Incorrect tagInfoId.{input.TagId.ToHex()}");
        Assert(State.TagInfoIdAddressListMap[input.WhitelistId][input.TagId].Value.Count == 0,
            $"Exist address list.{input.TagId.ToHex()}");
        State.ManagerTagInfoMap[input.ProjectId][input.WhitelistId].Value.Remove(input.TagId);
        var tagInfo = State.TagInfoMap[input.TagId];
        State.TagInfoMap.Remove(input.TagId);
        State.TagInfoIdAddressListMap[input.WhitelistId].Remove(input.TagId);
        foreach (var extraInfoId in whitelist.ExtraInfoIdList.Value)
        {
            if (extraInfoId.Id != input.TagId) continue;
            whitelist.ExtraInfoIdList.Value.Remove(extraInfoId);
            break;
        }
        Context.Fire(new TagInfoRemoved()
        {
            ProjectId = input.ProjectId,
            WhitelistId = input.WhitelistId,
            TagInfoId = input.TagId,
            TagInfo = tagInfo
        });
        return new Empty();
    }

    public override Empty AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
    {
        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
        var toAddExtraInfoIdList = new ExtraInfoIdList();
        foreach (var infoId in input.ExtraInfoIdList.Value)
        {
            AssertExtraInfoDuplicate(whitelistInfo.WhitelistId, infoId);
            var targetExtraInfoId = whitelistInfo.ExtraInfoIdList.Value.SingleOrDefault(i => i.Id == infoId.Id);
            if (targetExtraInfoId != null)
            {
                targetExtraInfoId.AddressList.Value.Add(infoId.AddressList.Value);
                toAddExtraInfoIdList = RecordChangedExtraInfoIdList(toAddExtraInfoIdList, infoId);
            }
            else
            {
                var toAdd = new ExtraInfoId
                {
                    Id = infoId.Id,
                    AddressList = infoId.AddressList
                };
                whitelistInfo.ExtraInfoIdList.Value.Add(toAdd);
                toAddExtraInfoIdList = RecordChangedExtraInfoIdList(toAddExtraInfoIdList, infoId);
            }

            if (infoId.Id == null) continue;
            //Whether tagId correct.
            if (!State.ManagerTagInfoMap[State.ProjectWhitelistIdMap[whitelistInfo.WhitelistId]][
                    whitelistInfo.WhitelistId].Value.Contains(infoId.Id))
            {
                throw new AssertionException($"Incorrect TagId.{infoId.Id}");
            }

            foreach (var address in infoId.AddressList.Value)
            {
                State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][address] = infoId.Id;
            }

            var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoId.Id] ?? new AddressList();
            addressList.Value.AddRange(infoId.AddressList.Value);
            State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoId.Id] = addressList;
        }

        State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
        Context.Fire(new WhitelistAddressInfoAdded()
        {
            WhitelistId = whitelistInfo.WhitelistId,
            ExtraInfoIdList = toAddExtraInfoIdList
        });

        return new Empty();
    }

    public override Empty RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
    {
        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
        if (whitelistInfo.StrategyType == StrategyType.Basic)
        {
            var toRemove = new ExtraInfoId
            {
                Id = null,
                AddressList = new AddressList()
            };
            var extraInfoId = whitelistInfo.ExtraInfoIdList.Value.SingleOrDefault(e => e.Id == null);
            if (extraInfoId != null)
            {
                var addressInWhitelist = extraInfoId.AddressList;
                var addressInput = input.ExtraInfoIdList.Value.Select(e => e.AddressList).ToList();
                foreach (var addressList in addressInput)
                {
                    foreach (var address in addressList.Value)
                    {
                        if (!addressInWhitelist.Value.Contains(address))
                        {
                            throw new AssertionException($"Address does not exist Or already been removed.{address}");
                        }

                        addressInWhitelist.Value.Remove(address);
                        toRemove.AddressList.Value.Add(address);
                    }
                }
            }
            else
            {
                throw new AssertionException($"No addressList.{input.ExtraInfoIdList}");
            }

            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList { Value = { toRemove } }
            });
            return new Empty();
        }

        //Format input=>{infoId,AddressList}
        var extraInfoIdList = input.ExtraInfoIdList.Value.GroupBy(e => e.Id).Select(g =>
        {
            var extraInfoId = new ExtraInfoId
            {
                Id = g.Key,
                AddressList = new AddressList()
            };
            var addressLists = g.Select(i => i.AddressList).ToList();
            foreach (var addressList in addressLists)
            {
                extraInfoId.AddressList.Value.AddRange(addressList.Value);
            }

            return extraInfoId;
        }).ToList();
        foreach (var infoId in extraInfoIdList)
        {
            //Whether tagId correct.
            if (!State.ManagerTagInfoMap[State.ProjectWhitelistIdMap[whitelistInfo.WhitelistId]][
                    whitelistInfo.WhitelistId].Value.Contains(infoId.Id))
            {
                throw new AssertionException($"Incorrect TagId.{infoId.Id}");
            }

            var targetExtraInfo = whitelistInfo.ExtraInfoIdList.Value.SingleOrDefault(i => i.Id == infoId.Id);
            if (targetExtraInfo == null)
            {
                throw new AssertionException($"No addressList under tagInfo id.{infoId}");
            }

            foreach (var address in infoId.AddressList.Value)
            {
                var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoId.Id];
                if (!targetExtraInfo.AddressList.Value.Contains(address))
                {
                    throw new AssertionException($"ExtraInfo does not exist Or already been removed.{address}");
                }

                targetExtraInfo.AddressList.Value.Remove(address);
                addressList.Value.Remove(address);
                State.AddressTagInfoIdMap[whitelistInfo.WhitelistId].Remove(address);
            }
        }

        Context.Fire(new WhitelistAddressInfoRemoved()
        {
            WhitelistId = whitelistInfo.WhitelistId,
            ExtraInfoIdList = new ExtraInfoIdList
            {
                Value = { extraInfoIdList }
            }
        });
        return new Empty();
    }

    public override Empty RemoveInfoFromWhitelist(RemoveInfoFromWhitelistInput input)
    {
        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
        var extraInfoIdList = new ExtraInfoIdList();
        foreach (var address in input.AddressList.Value)
        {
            extraInfoIdList.Value.Add(new ExtraInfoId
            {
                Id = State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][address] ?? null,
                AddressList = new AddressList
                {
                    Value = { address }
                }
            });
        }

        RemoveAddressInfoListFromWhitelist(new RemoveAddressInfoListFromWhitelistInput
        {
            WhitelistId = whitelistInfo.WhitelistId,
            ExtraInfoIdList = extraInfoIdList
        });
        return new Empty();
    }

    public override Empty DisableWhitelist(Hash input)
    {
        AssertWhitelistInfo(input);
        AssertWhitelistIsAvailable(input);
        var whitelistInfo = AssertWhitelistManager(input);
        whitelistInfo.IsAvailable = false;
        State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
        Context.Fire(new WhitelistDisabled
        {
            WhitelistId = whitelistInfo.WhitelistId,
            IsAvailable = whitelistInfo.IsAvailable,
        });
        return new Empty();
    }

    public override Empty EnableWhitelist(Hash input)
    {
        AssertWhitelistInfo(input);
        var whitelistInfo = AssertWhitelistManager(input);
        Assert(whitelistInfo.IsAvailable == false,
            $"The whitelist is already available.{whitelistInfo.WhitelistId}");
        whitelistInfo.IsAvailable = true;
        State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
        Context.Fire(new WhitelistReenable()
        {
            WhitelistId = whitelistInfo.WhitelistId,
            IsAvailable = whitelistInfo.IsAvailable,
        });
        return new Empty();
    }

    public override Empty ChangeWhitelistCloneable(ChangeWhitelistCloneableInput input)
    {
        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelistInfo = AssertWhitelistManager(input.WhitelistId);

        whitelistInfo.IsCloneable = input.IsCloneable;
        State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
        Context.Fire(new IsCloneableChanged()
        {
            WhitelistId = whitelistInfo.WhitelistId,
            IsCloneable = whitelistInfo.IsCloneable
        });
        return new Empty();
    }

    public override Empty UpdateExtraInfo(UpdateExtraInfoInput input)
    {
        if (input.ExtraInfoList == null)
        {
            throw new AssertionException("Address and extra info is null.");
        }

        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
        var projectId = State.ProjectWhitelistIdMap[whitelistInfo.WhitelistId];
        if (!State.ManagerTagInfoMap[projectId][whitelistInfo.WhitelistId].Value.Contains(input.ExtraInfoList.Id))
        {
            throw new AssertionException($"Incorrect extraInfoId.{input.ExtraInfoList.Id}");
        }

        var tagIdBefore = new Hash();
        foreach (var address in input.ExtraInfoList.AddressList.Value)
        {
            tagIdBefore = State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][address];
            var extraInfoAddressBefore = whitelistInfo.ExtraInfoIdList.Value.SingleOrDefault(i => i.Id == tagIdBefore);
            var extraInfoAddressAfter =
                whitelistInfo.ExtraInfoIdList.Value.SingleOrDefault(i => i.Id == input.ExtraInfoList.Id);
            if (extraInfoAddressBefore == null)
            {
                throw new AssertionException($"Incorrect address and extraInfoId.{tagIdBefore}");
            }

            extraInfoAddressBefore.AddressList.Value.Remove(address);
            if (extraInfoAddressAfter == null)
            {
                extraInfoAddressAfter = new ExtraInfoId()
                {
                    Id = input.ExtraInfoList.Id,
                    AddressList = new AddressList() { Value = { address } }
                };
                whitelistInfo.ExtraInfoIdList.Value.Add(extraInfoAddressAfter);
                //Add address to the new tagIdInfo map.
                var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][input.ExtraInfoList.Id] ??
                                  new AddressList();
                addressList.Value.Add(address);
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][input.ExtraInfoList.Id] = addressList;
            }
            else
            {
                extraInfoAddressAfter.AddressList.Value.Add(address);
                //Add address to the new tagIdInfo map.
                var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][input.ExtraInfoList.Id] ??
                                  new AddressList();
                addressList.Value.Add(address);
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][input.ExtraInfoList.Id] = addressList;
            }

            State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][address] = input.ExtraInfoList.Id;
            State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][tagIdBefore].Value.Remove(address);

        }

        Context.Fire(new ExtraInfoUpdated()
        {
            WhitelistId = input.WhitelistId,
            ExtraInfoIdBefore = new ExtraInfoId()
            {
                AddressList = input.ExtraInfoList.AddressList,
                Id = tagIdBefore
            },
            ExtraInfoIdAfter = new ExtraInfoId()
            {
                AddressList = input.ExtraInfoList.AddressList,
                Id = input.ExtraInfoList.Id
            }
        });

        return new Empty();
    }

    public override Empty TransferManager(TransferManagerInput input)
    {
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelist = AssertWhitelistManager(input.WhitelistId);
        Assert(!whitelist.Manager.Value.Contains(input.Manager) &&
               !State.ManagerListMap[whitelist.WhitelistId].Value.Contains(input.Manager),
            $"Manager already exists.{input}");
        whitelist.Manager.Value.Remove(Context.Sender);
        whitelist.Manager.Value.Add(input.Manager);
        var whitelistInfo = GetWhitelist(whitelist.WhitelistId);
        whitelistInfo.Manager.Value.Remove(Context.Sender);
        whitelistInfo.Manager.Value.Add(input.Manager);
        State.WhitelistInfoMap[whitelist.WhitelistId] = whitelist;
        State.ManagerListMap[whitelist.WhitelistId].Value.Remove(Context.Sender);
        State.ManagerListMap[whitelist.WhitelistId].Value.Add(input.Manager);
        Context.Fire(new ManagerTransferred()
        {
            WhitelistId = whitelist.WhitelistId,
            TransferFrom = Context.Sender,
            TransferTo = input.Manager
        });
        return new Empty();
    }

    public override Empty AddManagers(AddManagersInput input)
    {
        var whitelist = AssertWhitelistCreator(input.WhitelistId);
        var managerList = State.ManagerListMap[whitelist.WhitelistId];
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

        State.ManagerListMap[whitelist.WhitelistId] = managerList;
        SetWhitelistIdManager(whitelist.WhitelistId, addedManager);
        SetManagerListToWhitelist(whitelist.WhitelistId, addedManager);

        Context.Fire(new ManagerAdded
        {
            WhitelistId = whitelist.WhitelistId,
            ManagerList = addedManager
        });

        return new Empty();
    }

    public override Empty RemoveManagers(RemoveManagersInput input)
    {
        var whitelist = AssertWhitelistCreator(input.WhitelistId);
        var managerList = State.ManagerListMap[whitelist.WhitelistId];
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

        State.ManagerListMap[whitelist.WhitelistId] = managerList;
        RemoveWhitelistIdManager(whitelist.WhitelistId, removedList);
        RemoveManagerListFromWhitelist(whitelist.WhitelistId, removedList);
        Context.Fire(new ManagerRemoved()
        {
            WhitelistId = whitelist.WhitelistId,
            ManagerList = removedList
        });
        return new Empty();
    }

    public override Empty ResetWhitelist(ResetWhitelistInput input)
    {
        AssertWhitelistInfo(input.WhitelistId);
        AssertWhitelistIsAvailable(input.WhitelistId);
        var whitelist = AssertWhitelistManager(input.WhitelistId);
        Assert(State.ProjectWhitelistIdMap[whitelist.WhitelistId] == input.ProjectId,
            $"Incorrect projectId.{input.ProjectId}");
        if (whitelist.ExtraInfoIdList == null)
        {
            throw new AssertionException($"No extraInfo.{whitelist.WhitelistId}");
        }
        if (whitelist.StrategyType != StrategyType.Basic)
        {
            var idList = State.ManagerTagInfoMap[input.ProjectId][input.WhitelistId].Clone();
            if (idList == null)
            {
                throw new AssertionException($"No tagInfo.{whitelist.WhitelistId}");
            }

            var addressList = whitelist.ExtraInfoIdList.Value.Select(e => e.AddressList).ToList();
            State.ManagerTagInfoMap[input.ProjectId][input.WhitelistId].Value.Clear();
            foreach (var id in idList.Value)
            {
                State.TagInfoMap.Remove(id);
                State.TagInfoIdAddressListMap[input.WhitelistId].Remove(id);
            }
            foreach (var addresses in addressList)
            {
                foreach (var address in addresses.Value)
                {
                    State.AddressTagInfoIdMap[input.WhitelistId].Remove(address);
                }
            }
        }
        whitelist.ExtraInfoIdList.Value.Clear();
        Context.Fire(new WhitelistReset
        {
            WhitelistId = whitelist.WhitelistId,
            ProjectId = whitelist.ProjectId
        });
        return new Empty();
    }

    private ExtraInfoIdList RecordChangedExtraInfoIdList(ExtraInfoIdList toAddExtraInfoIdList, ExtraInfoId infoId)
    {
        var targetToAddList = toAddExtraInfoIdList.Value.SingleOrDefault(i => i.Id == infoId.Id);
        if (targetToAddList != null)
        {
            targetToAddList.AddressList.Value.Add(infoId.AddressList.Value);
        }
        else
        {
            var extraInfoId = new ExtraInfoId
            {
                Id = infoId.Id,
                AddressList = infoId.AddressList
            };
            toAddExtraInfoIdList.Value.Add(extraInfoId);
        }

        return toAddExtraInfoIdList;
    }
}