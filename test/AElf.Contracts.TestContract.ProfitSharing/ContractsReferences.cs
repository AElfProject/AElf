using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public partial class ProfitSharingContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
    }
}