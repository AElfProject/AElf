using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;

namespace AElf.Contracts.Association
{
    public partial class AssociationState
    {
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}