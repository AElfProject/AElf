using System.Linq;
using AElf.Types;

namespace AElf.Contracts.AssociationAuth
{
    public partial class AssociationAuthContract
    {
        private void CheckProposerAuthority(Address organizationAddress)
        {
            // Proposal should not be from multi sig account.
            // As a result, only check first public key.
            var organization = GetOrganization(organizationAddress);
            Reviewer reviewer = organization.Reviewers.FirstOrDefault(r => r.Address.Equals(Context.Sender));
            var proposerPerm = reviewer?.Weight ?? 0;
            Assert(Context.Sender.Equals(Context.Sender) &&
                   proposerPerm >= organization.ProposerThreshold, "Unable to propose.");
        }
        
        private bool IsReadyToRelease(ProposalInfo proposal, Organization organization)
        {
            return proposal.ApprovedWeight >= organization.ReleaseThreshold;
        }
    }
}