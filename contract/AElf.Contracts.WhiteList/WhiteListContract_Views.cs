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
            return State.WhitelistInfoMap[input];
        }

        public override SubscribeWhitelistInfo GetSubscribeWhitelist(Hash input)
        {
            return State.SubscribeWhitelistInfoMap[input];
        }

        public override ConsumedList GetConsumedList(Hash input)
        {
            return State.ConsumedListMap[input];
        }

        public override ExtraInfoList GetWhitelistDetail(Hash input)
        {
            AssertWhiteListInfo(input);
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
    }
}