using System.Linq;
using Google.Protobuf;

namespace AElf.Contracts.AssociationAuth
{
    public partial class AssociationAuthContract
    {
        private void CheckProposerAuthority(Address organizationAddress)
        {
            // Proposal should not be from multi sig account.
            // As a result, only check first public key.
            var organization = GetOrganization(organizationAddress);
            Reviewer reviewer = organization.Reviewers.FirstOrDefault(r =>
                r.PubKey.Equals(ByteString.CopyFrom(Context.RecoverPublicKey())));
            var proposerPerm = reviewer?.Weight ?? 0;
            Assert(Context.Sender.Equals(Context.Sender) &&
                   proposerPerm >= organization.ProposerThreshold, "Unable to propose.");

        }

        private void ValidateProposalContract()
        {
            if (State.ProposalContract.Value != null)
                return;
            State.ProposalContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.ProposalContractSystemName.Value);
        }
        
        private bool CheckApprovals(Hash proposalId, Address proposalOrganizationAddress)
        {
            ValidateProposalContract();
            var approved = State.ProposalContract.GetApprovedResult.Call(proposalId);

            var organization = GetOrganization(proposalOrganizationAddress);
            // processing approvals 
            var validApprovalCount = approved.Approvals.Aggregate((int) 0, (weights, approval) =>
            {
                var reviewer =
                    organization.Reviewers.FirstOrDefault(r =>
                        r.PubKey.SequenceEqual(approval.PublicKey.ToByteArray()));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovalCount >= organization.ReleaseThreshold;
        }
    }
}