using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public partial class AEDPoSContractState
    {
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal TokenHolderContractContainer.TokenHolderContractReferenceState TokenHolderContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
    }
}