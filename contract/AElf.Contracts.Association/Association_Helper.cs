using AElf.Types;

namespace AElf.Contracts.Association
{
    public partial class AssociationContract
    {
        private void AssertIsAuthorizedProposer(Organization organization, Address proposer)
        {
            Assert(organization.ProposerWhiteList.Proposers.Contains(proposer), "Unauthorized to propose.");
        }
        
        private void AssertIsAuthorizedOrganizationMember(Organization organization, Address member)
        {
            Assert(organization.OrganizationMemberList.OrganizationMembers.Contains(member),
                "Unauthorized member.");
        }

        private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization)
        {
            return proposal.ApprovedWeight >= organization.ReleaseThreshold;
        }

        private bool Validate(Organization organization)
        {
            var proposalReleaseThreshold = organization.ProposalReleaseThreshold;

            return proposalReleaseThreshold.MinimalVoteThreshold <= AbstractVoteTotal &&
                   proposalReleaseThreshold.MinimalApprovalThreshold <= proposalReleaseThreshold.MinimalVoteThreshold &&
                   proposalReleaseThreshold.MinimalApprovalThreshold > 0 &&
                   proposalReleaseThreshold.MaximalAbstentionThreshold >= 0 &&
                   proposalReleaseThreshold.MaximalRejectionThreshold >= 0 &&
                   proposalReleaseThreshold.MaximalAbstentionThreshold +
                   proposalReleaseThreshold.MinimalApprovalThreshold <= AbstractVoteTotal &&
                   proposalReleaseThreshold.MaximalRejectionThreshold +
                   proposalReleaseThreshold.MinimalApprovalThreshold <= AbstractVoteTotal;
        }

        private bool Validate(ProposalInfo proposal)
        {
            var validDestinationAddress = proposal.ToAddress != null;
            var validDestinationMethodName = !string.IsNullOrWhiteSpace(proposal.ContractMethodName);
            var validExpiredTime = proposal.ExpiredTime != null && Context.CurrentBlockTime < proposal.ExpiredTime;
            return validDestinationAddress && validDestinationMethodName && validExpiredTime;
        }

        private ProposalInfo GetValidProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Invalid proposal id.");
            Assert(Validate(proposal), "Invalid proposal.");
            return proposal;
        }

        private void AssertProposalNotYetVotedBySender(ProposalInfo proposal, Address sender)
        {
            var isAlreadyVoted = proposal.Approvals.Contains(sender) || proposal.Rejections.Contains(sender) ||
                                 proposal.Abstentions.Contains(sender);

            Assert(!isAlreadyVoted, "Sender already voted.");
        }
    }
}