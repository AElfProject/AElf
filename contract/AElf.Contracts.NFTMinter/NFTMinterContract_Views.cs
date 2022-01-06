using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMinter
{
    public partial class NFTMinterContract
    {
        public override BadgeInfo GetBadgeInfo(GetBadgeInfoInput input)
        {
            var badgeInfo = State.BadgeInfoMap[input.Symbol][input.TokenId];
            badgeInfo.Limit = State.MintLimitMap[input.Symbol][input.TokenId];
            return badgeInfo;
        }

        public override BoolValue IsInMintingWhiteList(IsInMintingWhiteListInput input)
        {
            return new BoolValue {Value = State.IsInWhiteListMap[input.Symbol][input.TokenId][input.Address]};
        }
    }
}