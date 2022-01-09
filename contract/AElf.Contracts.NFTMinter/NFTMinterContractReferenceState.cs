using AElf.Contracts.NFT;
using AElf.Standards.ACS6;

namespace AElf.Contracts.NFTMinter
{
    public partial class NFTMinterContractState
    {
        internal NFTContractContainer.NFTContractReferenceState NFTContract { get; set; }

        internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState
            RandomNumberProviderContract { get; set; }
    }
}