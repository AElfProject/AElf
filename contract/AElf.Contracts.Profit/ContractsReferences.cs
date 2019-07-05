using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}