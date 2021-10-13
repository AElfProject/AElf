using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Standards.ACS6;

namespace AElf.Contracts.StableToken
{
    public partial class StableTokenContractState
    {
        internal TokenHolderContractContainer.TokenHolderContractReferenceState TokenHolderContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState
            RandomNumberProviderContract { get; set; }
    }
}