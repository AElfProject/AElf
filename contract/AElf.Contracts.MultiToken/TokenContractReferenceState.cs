using AElf.Contracts.Association;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.Contracts.Treasury;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContractState
    {
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal AssociationContractContainer.AssociationContractReferenceState AssociationContract { get; set; }
        internal ReferendumContractContainer.ReferendumContractReferenceState ReferendumContract { get; set; }
    }
}