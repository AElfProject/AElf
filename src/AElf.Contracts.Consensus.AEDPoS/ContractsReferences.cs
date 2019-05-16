using AElf.Contracts.Election;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState
    {
        public SingletonState<Hash> ElectionContractSystemName { get; set; }

        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal Acs0.ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
    }
}