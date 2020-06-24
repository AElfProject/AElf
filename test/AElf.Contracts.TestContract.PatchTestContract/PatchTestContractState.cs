using AElf.Sdk.CSharp.State;

namespace AElf.TestContracts.PatchTestContract
{
    public partial class PatchTestContractState : ContractState
    {
        public SingletonState<int> Int32State { get; set; }
    }
}