using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Virgil.Crypto.Pfs;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        public override Hash CreateWhitelist(CreateWhitelistInput input)
        {
            var whitelistHash = CalculateWhitelistHash(Context.Sender,input.ProjectId);
            
            Assert(State.WhitelistInfoMap[whitelistHash] == null, $"Whitelist already exists.{whitelistHash.ToHex()}");

            var managerList = SetManagerList(whitelistHash, input.ManagerList);
            
            WhitelistInfo whitelistInfo;
            //Record duplicate list. 
            var duplicate = new List<ExtraInfo>();
            if (input.ExtraInfoList == null)
            {
                whitelistInfo = new WhitelistInfo
                {
                    WhitelistId = whitelistHash,
                    ProjectId = input.ProjectId,
                    Creator = Context.Sender,
                    IsAvailable = false,
                    IsCloneable = input.IsCloneable,
                    Remark = input.Remark,
                    Manager = managerList,
                    StrategyType = input.StrategyType
                };
            }
            else
            {
                //Remove duplicate addresses.
                var extraInfoList = input.ExtraInfoList.Value.GroupBy(e => e.Address)
                    .Select(e => e.FirstOrDefault()).ToList();

                if (input.StrategyType == StrategyType.Basic)
                {
                    var extraInfoIdList = extraInfoList.Select(e => new ExtraInfoId()
                    {
                        Address = e.Address
                    }).ToList();
                    whitelistInfo = new WhitelistInfo
                    {
                        WhitelistId = whitelistHash,
                        ProjectId = input.ProjectId,
                        ExtraInfoIdList = new ExtraInfoIdList() {Value = {extraInfoIdList}},
                        Creator = Context.Sender,
                        IsAvailable = true,
                        IsCloneable = input.IsCloneable,
                        Remark = input.Remark,
                        Manager = managerList,
                        StrategyType = input.StrategyType
                    };
                }
                else
                {
                    var extraInfoIdList = extraInfoList.Select(e =>
                    {
                        var id = CreateTagInfo(e.Info, input.ProjectId,whitelistHash);
                        //Set tagInfo list according to the owner and projectId.
                        var idList = State.ManagerTagInfoMap[Context.Sender][input.ProjectId][whitelistHash] ??
                                     new HashList();
                        idList.Value.Add(id);
                        State.ManagerTagInfoMap[Context.Sender][input.ProjectId][whitelistHash] = idList;
                        //Set address list according to the tagInfoId.
                        var addressList = State.TagInfoIdAddressListMap[whitelistHash][id] ?? new AddressList();
                        addressList.Value.Add(e.Address);
                        State.TagInfoIdAddressListMap[whitelistHash][id] = addressList;
                        //Map address and tagInfoId.
                        State.AddressTagInfoIdMap[whitelistHash][e.Address] = id;
                        return new ExtraInfoId()
                        {
                            Address = e.Address,
                            Id = id
                        };
                    }).ToList();
                    whitelistInfo = new WhitelistInfo
                    {
                        WhitelistId = whitelistHash,
                        ProjectId = input.ProjectId,
                        ExtraInfoIdList = new ExtraInfoIdList() {Value = {extraInfoIdList}},
                        Creator = Context.Sender,
                        IsAvailable = true,
                        IsCloneable = input.IsCloneable,
                        Remark = input.Remark,
                        Manager = managerList,
                        StrategyType = input.StrategyType
                    };
                }
                duplicate = input.ExtraInfoList.Value.Except(extraInfoList).ToList();
            }

            State.WhitelistInfoMap[whitelistHash] = whitelistInfo;
            SetWhitelistIdManager(whitelistHash, managerList);
            var projectWhitelist = State.WhitelistProjectMap[input.ProjectId] ?? new WhitelistIdList();
            projectWhitelist.WhitelistId.Add(whitelistHash);
            State.WhitelistProjectMap[input.ProjectId] = projectWhitelist;
            
            Context.Fire(new WhitelistCreated
            {
                WhitelistId = whitelistHash,
                ProjectId = whitelistInfo.ProjectId,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList,
                Creator = Context.Sender,
                IsCloneable = whitelistInfo.IsCloneable,
                IsAvailable = whitelistInfo.IsAvailable,
                Remark = whitelistInfo.Remark,
                Manager = whitelistInfo.Manager,
                StrategyType = whitelistInfo.StrategyType
            });
            Assert(duplicate.Count == 0 ,$"Duplicate address list.{duplicate}");
            return whitelistHash;
        }
        
        public override Hash AddExtraInfo(AddExtraInfoInput input)
        {
            if (input == null)
            {
                throw new AssertionException("Extra info is null");
            }

            MakeSureProjectCorrect(input.WhitelistId, input.ProjectId);
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            AssertWhitelistManager(input.WhitelistId);
            
            var tagInfoId = Context.Sender.CalculateExtraInfoId(input.ProjectId,input.TagInfo.TagName);
            Assert(State.TagInfoMap[tagInfoId] == null, $"The tag Info {input.TagInfo.TagName} already exists.");
            State.TagInfoMap[tagInfoId] = new TagInfo()
            {
                TagName = input.TagInfo.TagName,
                Info = input.TagInfo.Info
            };
            var idList = State.ManagerTagInfoMap[Context.Sender][input.ProjectId][input.WhitelistId] ?? new HashList();
            idList.Value.Add(tagInfoId);
            State.ManagerTagInfoMap[Context.Sender][input.ProjectId][input.WhitelistId] = idList;
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
            
            var extraInfoIdList = new ExtraInfoIdList();
            foreach (var address in input.AddressList.Value)
            {
                var extraInfoId = new ExtraInfoId()
                {
                    Address = address,
                    Id = tagInfoId
                };
                extraInfoIdList.Value.Add(extraInfoId);
            }
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
            AssertWhitelistManager(input.WhitelistId);
            
            Assert(State.ManagerTagInfoMap[Context.Sender][input.ProjectId][input.WhitelistId].Value.Contains(input.TagId),
                $"Incorrect tagInfoId.{input.TagId.ToHex()}");
            Assert(State.TagInfoIdAddressListMap[input.WhitelistId][input.TagId].Value.Count == 0,$"Exist address list.{input.TagId.ToHex()}");
            State.ManagerTagInfoMap[Context.Sender][input.ProjectId][input.WhitelistId].Value.Remove(input.TagId);
            var tagInfo = State.TagInfoMap[input.TagId];
            State.TagInfoMap.Remove(input.TagId);
            State.TagInfoIdAddressListMap[input.WhitelistId].Remove(input.TagId);
            Context.Fire(new TagInfoRemoved()
            {
                ProjectId = input.ProjectId,
                WhitelistId = input.WhitelistId,
                TagInfoId = input.TagId,
                TagInfo = tagInfo
            });
            return new Empty();
        }

        public override Empty AddAddressInfoToWhitelist(AddAddressInfoToWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            //Whether extraInfo exists.
            var extraInfoId = AssertExtraInfoDuplicate(whitelistInfo.WhitelistId,input.ExtraInfoId);
            whitelistInfo.ExtraInfoIdList.Value.Add(extraInfoId);
            if (input.ExtraInfoId.Id != null)
            {
                Assert(State.TagInfoMap[input.ExtraInfoId.Id] != null, $"TagInfo is not exist.{input.ExtraInfoId.Id}");
                var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][extraInfoId.Id] ?? new AddressList();
                addressList.Value.Add(extraInfoId.Address);
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][extraInfoId.Id] = addressList;
                State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][extraInfoId.Address] = extraInfoId.Id;
            }
            State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
            Context.Fire(new WhitelistAddressInfoAdded
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { extraInfoId }
                }
            });
            return new Empty();
        }
        
        public override Empty RemoveAddressInfoFromWhitelist(RemoveAddressInfoFromWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            if (input.ExtraInfoId.Id == null)
            {
                var addressList = whitelistInfo.ExtraInfoIdList.Value.Select(e => e.Address).ToList();
                var ifExistAddress = addressList.Contains(input.ExtraInfoId.Address);
                Assert(ifExistAddress,$"Address doesn't exist.{input.ExtraInfoId.Address}");
                whitelistInfo.ExtraInfoIdList.Value.Remove(input.ExtraInfoId);
            }
            else
            {
                var ifExist = whitelistInfo.ExtraInfoIdList.Value.Contains(input.ExtraInfoId);
                Assert(ifExist,$"ExtraInfo doesn't exist.{input.ExtraInfoId}");
                var toRemove = input.ExtraInfoId; 
                whitelistInfo.ExtraInfoIdList.Value.Remove(toRemove); 
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][toRemove.Id].Value.Remove(toRemove.Address);
                State.AddressTagInfoIdMap[whitelistInfo.WhitelistId].Remove(toRemove.Address);
            }
            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { input.ExtraInfoId }
                }
            });
            return new Empty();
        }
        
        public override Empty AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            //Remove duplicate addresses.
            var extraInfoId = input.ExtraInfoIdList.Value.GroupBy(e => e.Address)
                .Select(e => e.FirstOrDefault()).ToList();
            //Already added to the whitelist.
            var alreadyIn = new ExtraInfoIdList();
            foreach (var infoId  in from infoId in extraInfoId
                     where !whitelistInfo.ExtraInfoIdList.Value.Contains(infoId)
                     select infoId)
            {
                whitelistInfo.ExtraInfoIdList.Value.Add(infoId);
                alreadyIn.Value.Add(infoId);
                if (infoId.Id == null) continue;
                State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][infoId.Address] = infoId.Id;
                var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoId.Id] ?? new AddressList();
                addressList.Value.Add(infoId.Address);
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoId.Id] = addressList;
            }
            State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
            Context.Fire(new WhitelistAddressInfoAdded()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList
            });
            
            //Duplicate address list.
            var duplicate = input.ExtraInfoIdList.Value.Except(extraInfoId).ToList();
            //Remain extraInfo.
            var remain = extraInfoId.Except(alreadyIn.Value).ToList();
            Assert(duplicate.Count == 0,$"Duplicate address list.{duplicate}");
            Assert(remain.Count == 0,$"Duplicate extraInfo list.{remain}");
            
            return new Empty();
        }
        
        public override Empty RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            
            //Remove duplicate addresses.
            var extraInfoIdList = input.ExtraInfoIdList.Value.GroupBy(e => e.Address)
                .Select(e => e.FirstOrDefault()).ToList();
            var toRemoveList = new ExtraInfoIdList();
            var noMatch = new ExtraInfoIdList();
            foreach (var infoId in extraInfoIdList)
            { 
                var toRemove = whitelistInfo.ExtraInfoIdList.Value.Contains(infoId);
                if (toRemove)
                { 
                    whitelistInfo.ExtraInfoIdList.Value.Remove(infoId); 
                    toRemoveList.Value.Add(infoId);
                    if (infoId.Id == null) continue;
                    State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoId.Id].Value
                        .Remove(infoId.Address);
                    State.AddressTagInfoIdMap[whitelistInfo.WhitelistId].Remove(infoId.Address);
                }
                else
                {
                    noMatch.Value.Add(infoId);
                }
            }
            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = toRemoveList
            });
            var duplicate = input.ExtraInfoIdList.Value.Except(extraInfoIdList).ToList();
            Assert(duplicate.Count == 0,$"Duplicate addresses.{duplicate}");
            Assert(noMatch.Value.Count == 0,$"These extraInfos do not exist.{noMatch.Value}");
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
            Assert(State.TagInfoMap[input.ExtraInfoList.Id] != null,$"Incorrect extraInfoId.{input.ExtraInfoList.Id}");
            var matchInfoId =
                whitelistInfo.ExtraInfoIdList.Value
                    .FirstOrDefault(e => e.Address == input.ExtraInfoList.Address);
            if (matchInfoId != null)
            {
                matchInfoId.Id = input.ExtraInfoList.Id;
                State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
                
                //Update tagInfoId according to the address.
                var infoIdBefore = State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][matchInfoId.Address];
                State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][matchInfoId.Address] = input.ExtraInfoList.Id;
                
                //Remove address from the old tagIdInfo map.
                var ifExist = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoIdBefore].Value
                    .Contains(matchInfoId.Address);
                Assert(ifExist,$"No match address according to the tagInfoId.{infoIdBefore}");
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][infoIdBefore].Value.Remove(matchInfoId.Address);
                
                //Add address to the new tagIdInfo map.
                var addressList = State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][matchInfoId.Id] ?? new AddressList();
                addressList.Value.Add(matchInfoId.Address);
                State.TagInfoIdAddressListMap[whitelistInfo.WhitelistId][matchInfoId.Id] = addressList;
                Context.Fire(new ExtraInfoUpdated()
                {
                    WhitelistId = input.WhitelistId,
                    ExtraInfoIdBefore = new ExtraInfoId()
                    {
                        Address = matchInfoId.Address,
                        Id = infoIdBefore
                    },
                    ExtraInfoIdAfter = new ExtraInfoId()
                    {
                        Address = matchInfoId.Address,
                        Id = State.AddressTagInfoIdMap[whitelistInfo.WhitelistId][matchInfoId.Address]
                    }
                });
            }
            else
            {
                throw new AssertionException($"ExtraInfo not match.{input.ExtraInfoList.Address}");
            }
            
            return new Empty();
        }

        public override Empty TransferManager(TransferManagerInput input)
        {
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelist = AssertWhitelistManager(input.WhitelistId);
            whitelist.Manager.Value.Remove(Context.Sender);
            whitelist.Manager.Value.Add(input.Manager);
            State.WhitelistInfoMap[whitelist.WhitelistId] = whitelist;
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
            var remain = new AddressList();
            foreach (var manager in input.ManagerList.Value)
            {
                if (!managerList.Value.Contains(manager))
                {
                    managerList.Value.Add(manager);
                    addedManager.Value.Add(manager);
                }
                else
                {
                    remain.Value.Add(manager);
                }
            }

            State.ManagerListMap[whitelist.WhitelistId] = managerList;
            SetWhitelistIdManager(whitelist.WhitelistId, addedManager);
            
            Context.Fire(new ManagerAdded()
            {
                WhitelistId = whitelist.WhitelistId,
                ManagerList = addedManager
            });
            
            Assert(remain.Value.Count == 0,$"Managers already exists.{remain.Value}");
            return new Empty();
        }

        public override Empty RemoveManagers(RemoveManagersInput input)
        {
            var whitelist = AssertWhitelistCreator(input.WhitelistId);
            var managerList = State.ManagerListMap[whitelist.WhitelistId];
            var removedList = new AddressList();
            var remain = new AddressList();
            foreach (var manager in input.ManagerList.Value)
            {
                if (managerList.Value.Contains(manager))
                {
                    managerList.Value.Remove(manager);
                    removedList.Value.Add(manager);
                }
                else
                {
                    remain.Value.Add(manager);
                }
            }
            State.ManagerListMap[whitelist.WhitelistId] = managerList;
            RemoveWhitelistIdManager(whitelist.WhitelistId, removedList);
            Context.Fire(new ManagerRemoved()
            {
                WhitelistId = whitelist.WhitelistId,
                ManagerList = removedList
            });
            
            Assert(remain.Value.Count == 0,$"Managers doesn't exists.{remain.Value}");
            return new Empty();
        }

        public override Empty ResetWhitelist(ResetWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelist = AssertWhitelistManager(input.WhitelistId);
            whitelist.ExtraInfoIdList.Value.Clear();
            var idList = State.ManagerTagInfoMap[Context.Sender][input.ProjectId][input.WhitelistId];
            var addressList = whitelist.ExtraInfoIdList.Value.Select(e => e.Address).ToList();
            State.ManagerTagInfoMap[Context.Sender][input.ProjectId][input.WhitelistId].Value.Clear();
            foreach (var id in idList.Value)
            {
                State.TagInfoIdAddressListMap[input.WhitelistId].Remove(id);
            }
            foreach (var address in addressList)
            {
                State.AddressTagInfoIdMap[input.WhitelistId].Remove(address);
            }
            Context.Fire(new WhitelistReset()
            {
                WhitelistId = whitelist.WhitelistId
            });
            return new Empty();
        }
    }
}