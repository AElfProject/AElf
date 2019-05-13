using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState
    {
        public SingletonState<Hash> ElectionContractSystemName { get; set; }

        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}