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
            var consumedExtraList = State.ConsumedListMap[input].ExtraInfoIdList.Value.ToList();
            var subscribe = GetSubscribeWhitelist(input);
            var whitelist = GetWhitelist(subscribe.WhitelistId);
            var whitelistExtra = whitelist.ExtraInfoIdList.Value.ToList();
            var availableList = whitelistExtra.Where(info =>
                !consumedExtraList.Exists(extra => info.Address == extra.Address && info.Id == extra.Id)).ToList();
            return new WhitelistInfo()
            {
                WhitelistId = whitelist.WhitelistId,
                ExtraInfoIdList = new ExtraInfoIdList() {Value = { availableList }}
            };
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