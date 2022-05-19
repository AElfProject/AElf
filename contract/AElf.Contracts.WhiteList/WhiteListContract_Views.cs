using System.Linq;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        public override BytesValue GetExtraInfoByHash(Hash input)
        {
            return State.ExtraInfoMap[input];
        }

        public override WhitelistInfo GetWhitelist(Hash input)
        {
            var whitelist = AssertWhitelistInfo(input);
            return whitelist;
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

        public override ExtraInfoList GetWhitelistDetail(Hash input)
        {
            AssertWhitelistInfo(input);
            var whitelistInfo = GetWhitelist(input);
            return ConvertExtraInfoId(whitelistInfo.ExtraInfoIdList);
        }

        public override ExtraInfoList GetAvailableWhitelist(Hash input)
        {
            var subscribe = GetSubscribeWhitelist(input);
            var consumedList = State.ConsumedListMap[subscribe.SubscribeId];
            var whitelist = State.WhitelistInfoMap[subscribe.WhitelistId];
            var extraInfoList = ConvertExtraInfoId(whitelist.ExtraInfoIdList);
            if (consumedList.ExtraInfoIdList == null)
            {
                return extraInfoList;
            }
            else
            {
                var consumedExtraList = consumedList.ExtraInfoIdList.Value;
                var whitelistExtra = whitelist.ExtraInfoIdList.Value;
                var availableList = whitelistExtra.Except(consumedExtraList);
                return ConvertExtraInfoId(new ExtraInfoIdList() {Value = {availableList}});
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
            return State.WhitelistIdMap[input];
        }

        public override AddressList GetManagerList(Hash input)
        {
            return State.ManagerListMap[input];
        }

        public override ExtraInfoList GetExtraInfoByAddress(GetExtraInfoByAddressInput input)
        {
            var whitelist = GetWhitelist(input.WhitelistId);
            var extraInfoIds = whitelist.ExtraInfoIdList.Value.Where(info => info.Address == input.Address).ToList();
            var extraInfo = ConvertExtraInfoId(new ExtraInfoIdList(){Value = { extraInfoIds }});
            return extraInfo;
        }
    }
}