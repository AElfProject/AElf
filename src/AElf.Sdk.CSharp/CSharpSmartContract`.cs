using AElf.Sdk.CSharp.State;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> where TContractState : ContractState, new()
    {
        public CSharpSmartContractContext Context { get; private set; }

        public TContractState State { get; internal set; }

    }
}