using AElf.Contracts.Consensus.MinersCountProvider;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContractState
    {
        public SingletonState<Hash> ElectionContractSystemName { get; set; }
        public SingletonState<Hash> MinersCountProviderContractSystemName { get; set; }

        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal MinersCountProviderContractContainer.MinersCountProviderContractReferenceState MinersCountProviderContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}