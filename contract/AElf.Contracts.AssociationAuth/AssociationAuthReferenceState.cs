using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.AssociationAuth
{
    public partial class AssociationAuthState
    {
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}