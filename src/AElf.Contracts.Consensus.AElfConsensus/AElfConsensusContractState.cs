using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContractState : ContractState
    {
        public BoolState Initialized { get; set; }
    }
}