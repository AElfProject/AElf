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
            var extraInfoList = whitelistInfo.ExtraInfoIdList.Value.Select(info =>
            { 
                var extraInfo = GetExtraInfoByHash(info.Id).Value;
                return new ExtraInfo()
                {
                    Address = info.Address,
                    Info = extraInfo
                };
            }).ToList();
            return new ExtraInfoList() {Value = {extraInfoList}};
        }

        public override WhitelistInfo GetAvailableWhitelist(Hash input)
        {
            var subscribe = GetSubscribeWhitelist(input);
            var consumedList = State.ConsumedListMap[subscribe.SubscribeId];
            var whitelist = State.WhitelistInfoMap[subscribe.WhitelistId];
            if (consumedList.ExtraInfoIdList == null)
            {
                return new WhitelistInfo()
                {
                    WhitelistId = whitelist.WhitelistId,
                    ExtraInfoIdList = whitelist.ExtraInfoIdList
                };
            }
            else
            {
                var consumedExtraList = consumedList.ExtraInfoIdList.Value;
                var whitelistExtra = whitelist.ExtraInfoIdList.Value;
                var availableList = whitelistExtra.Except(consumedExtraList);
                return new WhitelistInfo()
                {
                    WhitelistId = whitelist.WhitelistId,
                    ExtraInfoIdList = new ExtraInfoIdList() {Value = { availableList }}
                };
            }
        }

        public override BoolValue GetFromAvailableWhitelist(GetFromAvailableWhitelistInput input)
        {
            var whitelist = GetAvailableWhitelist(input.SubscribeId);
            var extraInfoId = ConvertExtraInfo(input.ExtraInfo);
            var ifExist =  whitelist.ExtraInfoIdList.Value.Contains(extraInfoId);
            return new BoolValue() { Value = ifExist };
        }
    }
}