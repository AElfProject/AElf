using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.PoW
{
    public partial class PoWContractState : ContractState
    {
        public SingletonState<int> Difficulty { get; set; }
        public MappedState<long, long> Nonces { get; set; }
    }
}