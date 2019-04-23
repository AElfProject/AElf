using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContractState
    {
        public SingletonState<Hash> ElectionContractSystemName { get; set; }

        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}