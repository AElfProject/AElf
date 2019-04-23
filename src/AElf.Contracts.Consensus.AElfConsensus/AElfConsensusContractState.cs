using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public SingletonState<long> LockTokenForElection { get; set; }

        public SingletonState<bool> IsTermChangeable { get; set; }

        public SingletonState<bool> IsSideChain { get; set; }
    }
}