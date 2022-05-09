using AElf.Types;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract
    {
        public override ExtraInfo GetExtraInfoByHash(Hash input)
        {
            return State.ExtraInfoMap[input];
        }

        public override WhiteListInfo GetWhiteList(Hash input)
        {
            return State.WhiteListInfoMap[input];
        }

        public override SubscribeWhiteListInfo GetSubscribeWhiteList(Hash input)
        {
            return State.SubscribeWhiteListInfoMap[input];
        }

        public override ConsumedList GetConsumedList(Hash input)
        {
            return State.ConsumedListMap[input];
        }

        public override WhiteListInfo GetCloneWhiteList(Hash input)
        {
            return State.CloneWhiteListInfoMap[input];
        }
    }
}