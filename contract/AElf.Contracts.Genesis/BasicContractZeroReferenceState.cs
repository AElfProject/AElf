using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZeroState
    {
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}