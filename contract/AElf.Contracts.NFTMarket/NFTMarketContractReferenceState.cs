using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.Contracts.Whitelist;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContractState
    {
        internal NFTContractContainer.NFTContractReferenceState NFTContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        
        internal WhitelistContractContainer.WhitelistContractReferenceState WhitelistContract { get; set; }
    }
}