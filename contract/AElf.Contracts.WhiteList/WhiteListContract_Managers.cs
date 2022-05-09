using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract 
    {
        public override Hash CreateWhiteList(CreateWhiteListInput input)
        {
            Assert(input.AddressExtraInfoList != null, "The whiteList address and extra info is null");
            var whiteListHash = CalculateWhiteListHash(new AddressExtraInfoList
            {
                Value = { input.AddressExtraInfoList }
            });
            Assert(State.WhiteListInfoMap[whiteListHash] == null, "WhiteList already exists.");
            var addressExtraIdInfoList = input.AddressExtraInfoList?.Select(info =>
            {
                var addressExtraInfo = ConvertExtraInfo(info);
                return addressExtraInfo;
            }).ToList();
            var whiteListInfo = new WhiteListInfo
            {
                WhitelistId = whiteListHash,
                AddressExtraInfoList = new AddressExtraIdInfoList
                {
                    Value = {addressExtraIdInfoList}
                },
                IsAvailable = true,
                IsCloned = input.IsCloned,
                Remark = input.Remark
            };
            State.WhiteListInfoMap[whiteListHash] = whiteListInfo;
            Context.Fire(new WhiteListCreated
            {
                WhiteListId = whiteListHash,
                AddressExtraInfoList = whiteListInfo.AddressExtraInfoList,
                IsAvailable = whiteListInfo.IsAvailable,
                IsCloned = whiteListInfo.IsCloned,
                Remark = whiteListInfo.Remark
            });
            return whiteListHash;
        }

        public override Empty AddAddressInfoToWhiteList(AddAddressInfoToWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var addressExtraIdInfo = ConvertExtraInfo(input.AddressExtraInfo);
            whiteListInfo.AddressExtraInfoList.Value.Add(addressExtraIdInfo);
            State.WhiteListInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            Context.Fire(new WhiteListAddressInfoAdded
            {
                WhitelistId = whiteListInfo.WhitelistId,
                AddressExtraInfoList = new AddressExtraIdInfoList
                {
                    Value = {addressExtraIdInfo}
                }
            });
            return new Empty();
        }

        public override Empty RemoveAddressInfoFromWhiteList(RemoveAddressInfoFromWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var addressExtraInfo = RemoveAddressOrExtra(whiteListInfo, input.AddressExtraInfo);
            Context.Fire(new WhiteListAddressInfoRemoved()
            {
                WhitelistId = whiteListInfo.WhitelistId,
                AddressExtraInfoList = new AddressExtraIdInfoList
                {
                    Value = {addressExtraInfo}
                }
            });
            return new Empty();
        }

        public override Empty AddAddressInfoListToWhiteList(AddAddressInfoListToWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            foreach (var addressExtraInfo in input.AddressExtraInfoList)
            {
                var extraInfo = ConvertExtraInfo(addressExtraInfo);
                whiteListInfo.AddressExtraInfoList.Value.Add(extraInfo);
            }

            State.WhiteListInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            Context.Fire(new WhiteListAddressInfoAdded()
            {
                WhitelistId = whiteListInfo.WhitelistId,
                AddressExtraInfoList = whiteListInfo.AddressExtraInfoList
            });
            return new Empty();
        }

        public override Empty RemoveAddressInfoListFromWhiteList(RemoveAddressInfoListFromWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var addressExtraInfoList = new AddressExtraIdInfoList();
            foreach (var info in input.AddressExtraInfoList)
            {
                var addressExtraInfo = RemoveAddressOrExtra(whiteListInfo, info);
                addressExtraInfoList.Value.Add(addressExtraInfo);
            }

            Context.Fire(new WhiteListAddressInfoRemoved()
            {
                WhitelistId = whiteListInfo.WhitelistId,
                AddressExtraInfoList = addressExtraInfoList
            });
            return new Empty();
        }


        public override Empty DisableWhiteList(DisableWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            whiteListInfo.IsAvailable = false;
            State.WhiteListInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            Context.Fire(new WhiteListDisabled
            {
                WhitelistId = whiteListInfo.WhitelistId,
                IsAvailable = whiteListInfo.IsAvailable,
                Remark = input.Remark
            });
            return new Empty();
        }


        public override Empty SetExtraInfo(SetExtraInfoInput input)
        {
            Assert(State.ExtraInfoMap[input.ExtraInfoId] != null, "Extra Info doesn't exist.");
            State.ExtraInfoMap[input.ExtraInfoId].ExtraInfo_ = input.ExtraInfo;
            Context.Fire(new SetExtraInfo
            {
                ExtraInfoId = input.ExtraInfoId,
                ExtraInfo = State.ExtraInfoMap[input.ExtraInfoId].ExtraInfo_
            });
            return new Empty();
        }

    }

}
