using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Treasury;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState
    {
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        internal ParliamentAuth.ParliamentAuthContractContainer.ParliamentAuthContractReferenceState
            ParliamentAuthContract { get; set; }
    }
}