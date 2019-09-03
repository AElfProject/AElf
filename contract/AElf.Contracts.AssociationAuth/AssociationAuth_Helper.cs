using System.Linq;
using AElf.Types;

namespace AElf.Contracts.AssociationAuth
{
    public partial class AssociationAuthContract
    {
        private void AssertSenderIsAuthorizedProposer(Organization organization)
        {
            // Proposal should not be from multi sig account.
            // As a result, only check first public key.
            var reviewer = GetReviewerObjectForSender(organization);
            var authorizedProposer = reviewer.Weight >= organization.ProposerThreshold;
            Assert(authorizedProposer, "Unable to propose.");
        }

        private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization)
        {
            return proposal.ApprovedWeight >= organization.ReleaseThreshold;
        }

        private bool Validate(Organization organization)
        {
            var allReviewersHaveValidWeigths = organization.Reviewers.All(r => r.Weight >= 0);
            var withValidProposer = organization.Reviewers.Any(r => r.Weight >= organization.ProposerThreshold);
            var withValidReleaseThreshold =
                organization.Reviewers.Sum(reviewer => reviewer.Weight) > organization.ReleaseThreshold;
            return allReviewersHaveValidWeigths && withValidProposer && withValidReleaseThreshold;
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

        private void AssertProposalNotYetApprovedBySender(ProposalInfo proposal)
        {
            Assert(!proposal.ApprovedReviewer.Contains(Context.Sender), "Already approved.");
        }

        private Reviewer GetReviewerObjectForSender(Organization organization)
        {
            var reviewer = organization.Reviewers.FirstOrDefault(r => r.Address.Equals(Context.Sender));
            Assert(reviewer != null, "Unauthorized approval.");
            return reviewer;
        }
    }
}