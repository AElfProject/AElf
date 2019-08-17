using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContractState : ContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}