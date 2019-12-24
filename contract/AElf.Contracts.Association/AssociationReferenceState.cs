using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.Association
{
    public partial class AssociationState
    {
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}