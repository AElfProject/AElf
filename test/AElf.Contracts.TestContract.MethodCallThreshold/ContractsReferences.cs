using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Treasury;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public partial class ProfitSharingContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
    }
}