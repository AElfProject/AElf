using AElf.Contracts.Election;
using AElf.Contracts.Treasury;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState
    {
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
    }
}