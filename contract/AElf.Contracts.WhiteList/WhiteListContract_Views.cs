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

        public override WhitelistInfo GetCloneWhitelist(Hash input)
        {
            return State.CloneWhitelistInfoMap[input];
        }
    }
}