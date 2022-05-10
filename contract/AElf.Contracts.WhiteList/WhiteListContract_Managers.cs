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
                throw new AssertionException("The whiteList address and extra info is null");
            }

            var whiteListHash = CalculateWhitelistHash(new ExtraInfoList
            {
                Value = { input.ExtraInfoList.Value }
            });
            Assert(State.WhitelistInfoMap[whiteListHash] == null, "WhiteList already exists.");
            var extraInfoIdList = input.ExtraInfoList.Value.Select(info =>
            {
                var addressExtraInfo = ConvertExtraInfo(info);
                return addressExtraInfo;
            }).ToList();
            var whitelistInfo = new WhitelistInfo
            {
                WhitelistId = whiteListHash,
                ExtraInfoIdList = new ExtraInfoIdList
                {
                    Value = { extraInfoIdList }
                },
                IsAvailable = true,
                IsCloneable = input.IsCloneable,
                Remark = input.Remark
            };
            State.WhitelistInfoMap[whiteListHash] = whitelistInfo;
            Context.Fire(new WhitelistCreated
            {
                WhitelistId = whiteListHash,
                ExtraInfoIdList = whitelistInfo.ExtraInfoIdList,
                IsAvailable = whitelistInfo.IsAvailable,
                Remark = whitelistInfo.Remark
            });
            return whiteListHash;
        }

        public override Empty AddAddressInfoToWhitelist(AddAddressInfoToWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var addressExtraIdInfo = ConvertExtraInfo(input.ExtraInfo);
            whiteListInfo.ExtraInfoIdList.Value.Add(addressExtraIdInfo);
            State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            Context.Fire(new WhitelistAddressInfoAdded
            {
                WhitelistId = whiteListInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { addressExtraIdInfo }
                }
            });
            return new Empty();
        }

        public override Empty RemoveAddressInfoFromWhitelist(RemoveAddressInfoFromWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var extraInfo = RemoveAddressOrExtra(whiteListInfo, input.ExtraInfo);
            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whiteListInfo.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList()
                {
                    Value = { extraInfo }
                }
            });
            return new Empty();
        }

        public override Empty AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            foreach (var addressExtraInfo in input.ExtraInfoList.Value)
            {
                var extraInfo = ConvertExtraInfo(addressExtraInfo);
                whiteListInfo.ExtraInfoIdList.Value.Add(extraInfo);
            }

            State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            Context.Fire(new WhitelistAddressInfoAdded()
            {
                WhitelistId = whiteListInfo.WhitelistId,
                ExtraInfoIdList = whiteListInfo.ExtraInfoIdList
            });
            return new Empty();
        }

        public override Empty RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            var extraInfoIdList = new ExtraInfoIdList();
            foreach (var info in input.ExtraInfoList.Value)
            {
                var addressExtraInfo = RemoveAddressOrExtra(whiteListInfo, info);
                extraInfoIdList.Value.Add(addressExtraInfo);
            }

            Context.Fire(new WhitelistAddressInfoRemoved()
            {
                WhitelistId = whiteListInfo.WhitelistId,
                ExtraInfoIdList = extraInfoIdList
            });
            return new Empty();
        }


        public override Empty DisableWhitelist(DisableWhitelistInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhitelistId);
            whiteListInfo.IsAvailable = false;
            State.WhitelistInfoMap[whiteListInfo.WhitelistId] = whiteListInfo;
            Context.Fire(new WhitelistDisabled
            {
                WhitelistId = whiteListInfo.WhitelistId,
                Remark = input.Remark
            });
            return new Empty();
        }

        public override Empty SetExtraInfo(SetExtraInfoInput input)
        {
            Assert(State.ExtraInfoMap[input.ExtraInfoId] != null, "Extra Info doesn't exist.");
            State.ExtraInfoMap[input.ExtraInfoId].Value = input.ExtraInfo;
            Context.Fire(new SetExtraInfo
            {
                ExtraInfoId = input.ExtraInfoId,
                ExtraInfo = State.ExtraInfoMap[input.ExtraInfoId].Value
            });
            return new Empty();
        }
    }
}