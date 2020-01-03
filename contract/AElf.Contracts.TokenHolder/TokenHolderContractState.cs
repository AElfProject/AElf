using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TokenHolder
{
    public partial class TokenHolderContractState : ContractState
    {
        public MappedState<Address, TokenHolderProfitScheme> TokenHolderProfitSchemes { get; set; }
        
    }
}