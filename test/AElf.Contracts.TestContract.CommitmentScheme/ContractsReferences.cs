using AElf.Contracts.Consensus.AEDPoS;

namespace AElf.Contracts.TestContract.CommitmentScheme
{
    public partial class CommitmentSchemeContractState
    {
        internal AEDPoSContractContainer.AEDPoSContractReferenceState AEDPoSContract { get; set; }
    }
}