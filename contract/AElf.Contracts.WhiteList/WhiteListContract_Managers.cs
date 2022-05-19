using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Whitelist.Extensions;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        public override Hash CreateWhitelist(CreateWhitelistInput input)
        {
            if (input.ExtraInfoList == null)
            {
                throw new AssertionException("The whitelist address and extra info is null");
            }

            var whitelistHash = CalculateWhitelistHash(Context.Sender,new ExtraInfoList
            {
                Value = { input.ExtraInfoList.Value }
            });
            
            Assert(State.WhitelistInfoMap[whitelistHash] == null, $"Whitelist already exists.{whitelistHash.ToHex()}");
            var extraInfoIdList = input.ExtraInfoList.Value.Select(info =>
            {
                var addressExtraInfo = ConvertExtraInfo(info);
                return addressExtraInfo;
            }).ToList();

            var managerList = SetManagerList(whitelistHash, input.ManagerList);
            
            var whitelistInfo = new WhitelistInfo
            {
                WhitelistId = whitelistHash,
                ExtraInfoIdList = new ExtraInfoIdList
                {
                    Value = { extraInfoIdList }
                },
                Creator = Context.Sender,
                IsAvailable = true,
                IsCloneable = input.IsCloneable,
                Remark = input.Remark,
                Manager = managerList
            };
            State.WhitelistInfoMap[whitelistHash] = whitelistInfo;

            SetWhitelistIdManager(whitelistHash, managerList);

            Context.Fire(new WhitelistCreated
            {
                WhitelistId = whitelistHash,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList,
                Creator = Context.Sender,
                IsCloneable = whitelistInfo.IsCloneable,
                IsAvailable = whitelistInfo.IsAvailable,
                Remark = whitelistInfo.Remark,
                Manager = whitelistInfo.Manager
            });
            return whitelistHash;
        }

        public override Empty AddAddressInfoToWhitelist(AddAddressInfoToWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            //Whether extraInfo exists.
            var extraInfoId = AssertExtraInfoIsExist(whitelistInfo.WhitelistId,input.ExtraInfo);
            
            whitelistInfo.ExtraInfoIdList.Value.Add(extraInfoId);
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
            
            var extraInfoId = RemoveAddressOrExtra(whitelistInfo, input.ExtraInfo);
            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { extraInfoId }
                }
            });
            return new Empty();
        }

        public override Empty AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            //Already added to the whitelist.
            var alreadyIn = new ExtraInfoList();
            
            foreach (var addressExtraInfo in input.ExtraInfoList.Value)
            {
                var extraInfoId = ConvertExtraInfo(addressExtraInfo);
                var ifExist = whitelistInfo.ExtraInfoIdList.Value.Contains(extraInfoId);
                if (!ifExist)
                {
                    whitelistInfo.ExtraInfoIdList.Value.Add(extraInfoId);
                    alreadyIn.Value.Add(addressExtraInfo);
                }
            }
            State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
            Context.Fire(new WhitelistAddressInfoAdded()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList
            });
            
            //Remain extraInfo.
            var remain = input.ExtraInfoList.Value.Except(alreadyIn.Value).ToList();
            Assert(remain.Count == 0,$"These extraInfo already exists in the whitelist.{remain}");
            
            return new Empty();
        }

        public override Empty RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            
            var extraInfoIdList = new ExtraInfoIdList();
            foreach (var info in input.ExtraInfoList.Value)
            {
                var extraInfoId = RemoveAddressOrExtra(whitelistInfo, info);
                extraInfoIdList.Value.Add(extraInfoId);
            }

            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = extraInfoIdList
            });
            return new Empty();
        }


        public override Empty DisableWhitelist(DisableWhitelistInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelistInfo = AssertWhitelistManager(input.WhitelistId);
            whitelistInfo.IsAvailable = false;
            State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
            Context.Fire(new WhitelistDisabled
            {
                WhitelistId = whitelistInfo.WhitelistId,
                IsAvailable = whitelistInfo.IsAvailable,
                Remark = input.Remark
            });
            return new Empty();
        }

        public override Empty ChangeWhitelistCloneable(ChangeWhitelistCloneableInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistManager(input.WhitelistId);
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

        public override Empty AddExtraInfo(AddExtraInfoInput input)
        {
            if (input == null)
            {
                throw new AssertionException("Extra info is null");
            }
            Assert(State.ExtraInfoMap[input.ExtraInfoId] == null, "Extra Info is exist.");
            State.ExtraInfoMap[input.ExtraInfoId] = new BytesValue()
            {
                Value = input.ExtraInfo
            };
            Context.Fire(new ExtraInfoAdded
            {
                ExtraInfoId = input.ExtraInfoId,
                ExtraInfo = State.ExtraInfoMap[input.ExtraInfoId].Value
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
            var extraInfoList = whitelistInfo.ExtraInfoIdList.Value.Select(info =>
            {
                var matchInfo = new ExtraInfoUpdate();
                foreach (var inputValue in input.ExtraInfoList.Value)
                {
                    var extraInfoIdBefore = inputValue.InfoBefore.CalculateExtraInfoId();
                    var extraInfoIdUpdate = inputValue.InfoUpdate.CalculateExtraInfoId();
                    //Select match address and extraInfoId , update extraInfo.
                    if (inputValue.Address.Equals(info.Address) && extraInfoIdBefore.Equals(info.Id))
                    {
                        
                        info.Id = extraInfoIdUpdate;
                        matchInfo.Address = inputValue.Address;
                        matchInfo.InfoBefore = inputValue.InfoBefore;
                        matchInfo.InfoUpdate = inputValue.InfoUpdate;
                    }
                }
                return matchInfo;
            }).ToList();
            var remain = input.ExtraInfoList.Value.Except(extraInfoList).ToList();
            Assert( remain.Count == 0,$"No match address.{input.ExtraInfoList.Value}");
            Context.Fire(new UpdateWhitelist()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoList = new ExtraInfoUpdateList()
                {
                    Value = { extraInfoList }
                }
            });
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
    }
}