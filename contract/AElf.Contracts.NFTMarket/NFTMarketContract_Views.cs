namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override ListedNFTInfo GetListedNFTInfo(GetListedNFTInfoInput input)
        {
            return State.ListedNftInfoMap[input.Symbol][input.TokenId][input.Owner];
        }
    }
}