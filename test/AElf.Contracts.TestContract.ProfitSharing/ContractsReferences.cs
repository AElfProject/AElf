using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public partial class ProfitSharingContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
    }
}