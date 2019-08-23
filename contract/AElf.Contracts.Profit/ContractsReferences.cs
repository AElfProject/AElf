using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}