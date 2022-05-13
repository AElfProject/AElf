using System.Collections.Generic;
using System.Linq;
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
            Assert(State.WhitelistInfoMap[whitelistHash] == null, "WhiteList already exists.");
            var extraInfoIdList = input.ExtraInfoList.Value.Select(info =>
            {
                var addressExtraInfo = ConvertExtraInfo(info);
                return addressExtraInfo;
            }).ToList();
            var whitelistInfo = new WhitelistInfo
            {
                WhitelistId = whitelistHash,
                ExtraInfoIdList = new ExtraInfoIdList
                {
                    Value = { extraInfoIdList }
                },
                IsAvailable = true,
                IsCloneable = input.IsCloneable,
                Remark = input.Remark,
                Manager = Context.Sender
            };
            State.WhitelistInfoMap[whitelistHash] = whitelistInfo;
            Context.Fire(new WhitelistCreated
            {
                WhitelistId = whitelistHash,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList,
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
            var remain = input.ExtraInfoList.Value.Except(alreadyIn.Value).ToList();
            State.WhitelistInfoMap[whitelistInfo.WhitelistId] = whitelistInfo;
            Context.Fire(new WhitelistAddressInfoAdded()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList
            });
            Assert(remain.Count == 0,$"These extraInfo already exists.{remain}");
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

        public override Empty ChangeWhitelistCloneable(UpdateWhitelistCloneableInput input)
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
                var matchInfo = new ExtraInfoId();
                foreach (var inputValue in input.ExtraInfoList.Value)
                {
                    //Select match address and extraInfoId , update extraInfo.
                    if (inputValue.Address.Equals(info.Address) )
                    {
                        var extraInfoId = ConvertExtraInfo(inputValue);
                        info.Id = extraInfoId.Id;
                        matchInfo.Address = inputValue.Address;
                        matchInfo.Id = extraInfoId.Id;
                        input.ExtraInfoList.Value.Remove(inputValue);
                    }
                    break;
                }
                return matchInfo;
            }).ToList();
            Assert(input.ExtraInfoList.Value.Count == 0,$"No match address.{input.ExtraInfoList.Value}");
            Context.Fire(new UpdateWhitelist()
            {
                WhitelistId = whitelistInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { extraInfoList }
                }
            });
            return new Empty();
        }

        public override Empty TransferManager(TransferManagerInput input)
        {
            AssertWhitelistInfo(input.WhitelistId);
            AssertWhitelistIsAvailable(input.WhitelistId);
            var whitelist = AssertWhitelistManager(input.WhitelistId);
            whitelist.Manager = input.Manager;
            State.WhitelistInfoMap[whitelist.WhitelistId] = whitelist;
            Context.Fire(new ManagerTransferred()
            {
                WhitelistId = whitelist.WhitelistId,
                TransferFrom = Context.Sender,
                TransferTo = whitelist.Manager
            });
            return new Empty();
        }
    }
}