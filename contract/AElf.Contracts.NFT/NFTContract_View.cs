using AElf.Types;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        public override NFTInfo GetNFTInfo(GetNFTInfoInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            return State.NftInfoMap[tokenHash];
        }

        public override NFTInfo GetNFTInfoByTokenHash(Hash input)
        {
            return State.NftInfoMap[input];
        }
    }
}