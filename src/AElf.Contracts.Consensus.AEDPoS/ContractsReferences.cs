using AElf.Contracts.Election;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState
    {
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
    }
}