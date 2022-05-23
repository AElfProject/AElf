using System.Linq;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
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
            Assert(State.TagInfoMap[input] != null,$"Not found tag.{input.ToHex()}");
            return State.TagInfoMap[input];
        }

        public override ExtraInfoList GetExtraInfoByTag(GetExtraInfoByTagInput input)
        {
            var whitelist = GetWhitelist(input.WhitelistId);
            var tagInfo = GetTagInfoByHash(input.TagInfoId);
            Assert(State.TagInfoIdAddressListMap[whitelist.WhitelistId][input.TagInfoId] != null,$"No address list under the current tag.{input.TagInfoId.ToHex()}");
            var addressList = State.TagInfoIdAddressListMap[whitelist.WhitelistId][input.TagInfoId];
            var extraInfoList = new ExtraInfoList();
            foreach (var address in addressList.Value)
            {
                var extraInfo = new ExtraInfo()
                {
                    Address = address,
                    Info = tagInfo
                };
                extraInfoList.Value.Add(extraInfo);
            }

            return extraInfoList;
        }

        public override HashList GetExtraInfoIdList(GetExtraInfoIdListInput input)
        {
            var whitelist = GetWhitelist(input.WhitelistId);
            var idList = State.ManagerTagInfoMap[input.Owner][input.ProjectId][whitelist.WhitelistId];
            Assert(idList.Value.Count != 0,$"No extraInfo id list.{input.Owner}{input.ProjectId.ToHex()}{input.WhitelistId.ToHex()}");
            return idList;
        }
        
        public override TagInfo GetExtraInfoByAddress(GetExtraInfoByAddressInput input)
        {
            var whitelist = GetWhitelist(input.WhitelistId);
            var tagInfoId = State.AddressTagInfoIdMap[whitelist.WhitelistId][input.Address];
            Assert(!tagInfoId.Value.IsEmpty,$"No Match tagInfo according to the address.{input.WhitelistId.ToHex()}{input.Address}");
            var tagInfo = GetTagInfoByHash(tagInfoId);
            return tagInfo;
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

        public override ExtraInfoList GetAvailableWhitelist(Hash input)
        {
            var subscribe = GetSubscribeWhitelist(input);
            var consumedList = State.ConsumedListMap[subscribe.SubscribeId];
            var whitelist = State.WhitelistInfoMap[subscribe.WhitelistId];
            var extraInfoList = ConvertToInfoList(whitelist.ExtraInfoIdList);
            if (consumedList.ExtraInfoIdList == null)
            {
                return extraInfoList;
            }
            else
            {
                var consumedExtraList = consumedList.ExtraInfoIdList.Value;
                var whitelistExtra = whitelist.ExtraInfoIdList.Value;
                var availableList = whitelistExtra.Except(consumedExtraList);
                return ConvertToInfoList(new ExtraInfoIdList() {Value = {availableList}});
            }
        }

        public override BoolValue GetFromAvailableWhitelist(GetFromAvailableWhitelistInput input)
        {
            var whitelist = GetAvailableWhitelist(input.SubscribeId);
            var ifExist =  whitelist.Value.Contains(input.ExtraInfo);
            return new BoolValue() { Value = ifExist };
        }

        public override WhitelistIdList GetWhitelistByManager(Address input)
        {
            var whitelistIdList = State.WhitelistIdMap[input];
            Assert(whitelistIdList.WhitelistId.Count != 0,$"No whitelist according to the manager.{input}");
            return State.WhitelistIdMap[input];
        }

        public override AddressList GetManagerList(Hash input)
        {
            return State.ManagerListMap[input];
        }

        
    }
}