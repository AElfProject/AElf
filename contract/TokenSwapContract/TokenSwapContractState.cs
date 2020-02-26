using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Tokenswap;

namespace TokenSwapContract
{
    public class TokenSwapContractState : ContractState
    {
        public MappedState<Hash, SwapPair> SwapPairs { get; set; }
        
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}