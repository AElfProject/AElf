using System.Linq;
using Acs3;
using AElf.Types;

namespace AElf.Contracts.Association
{
    public partial class AssociationContract
    {
        private void AssertIsAuthorizedProposer(Organization organization, Address proposer)
        {
            Assert(organization.ProposerWhiteList.Contains(proposer), "Unauthorized to propose.");
        }
        
        private void AssertIsAuthorizedOrganizationMember(Organization organization, Address member)
        {
            Assert(organization.OrganizationMemberList.Contains(member),
                "Unauthorized member.");
        }

        private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization)
        {
            var isRejected = IsProposalRejected(proposal, organization);
            if (isRejected)
                return false;

            var isAbstained = IsProposalAbstained(proposal, organization);
            return !isAbstained && CheckEnoughVoteAndApprovals(proposal, organization);
        }
        
        private bool IsProposalRejected(ProposalInfo proposal, Organization organization)
        {
            var rejectionMemberCount =
                proposal.Rejections.Count(organization.OrganizationMemberList.Contains);
            return rejectionMemberCount > organization.ProposalReleaseThreshold.MaximalRejectionThreshold;
        }

        private bool IsProposalAbstained(ProposalInfo proposal, Organization organization)
        {
            var abstentionMemberCount = proposal.Abstentions.Count(organization.OrganizationMemberList.Contains);
            return abstentionMemberCount > organization.ProposalReleaseThreshold.MaximalAbstentionThreshold;
        }

        private bool CheckEnoughVoteAndApprovals(ProposalInfo proposal, Organization organization)
        {
            var approvedMemberCount = proposal.Approvals.Count(organization.OrganizationMemberList.Contains);
            var isApprovalEnough =
                approvedMemberCount >= organization.ProposalReleaseThreshold.MinimalApprovalThreshold;
            if (!isApprovalEnough)
                return false;

            var isVoteThresholdReached =
                proposal.Abstentions.Concat(proposal.Approvals).Concat(proposal.Rejections).Count() >=
                organization.ProposalReleaseThreshold.MinimalVoteThreshold;
            return isVoteThresholdReached;
        }

        private bool Validate(Organization organization)
        {
            if (organization.ProposerWhiteList.Empty() || organization.OrganizationMemberList.Empty())
                return false;
            
            var proposalReleaseThreshold = organization.ProposalReleaseThreshold;
            var organizationMemberCount = organization.OrganizationMemberList.Count();
            return proposalReleaseThreshold.MinimalVoteThreshold <= organizationMemberCount &&
                   proposalReleaseThreshold.MinimalApprovalThreshold <= proposalReleaseThreshold.MinimalVoteThreshold &&
                   proposalReleaseThreshold.MinimalApprovalThreshold > 0 &&
                   proposalReleaseThreshold.MaximalAbstentionThreshold >= 0 &&
                   proposalReleaseThreshold.MaximalRejectionThreshold >= 0 &&
                   proposalReleaseThreshold.MaximalAbstentionThreshold +
                   proposalReleaseThreshold.MinimalApprovalThreshold <= organizationMemberCount &&
                   proposalReleaseThreshold.MaximalRejectionThreshold +
                   proposalReleaseThreshold.MinimalApprovalThreshold <= organizationMemberCount;
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
   
        private OrganizationHashAddressPair CalculateOrganizationHashAddressPair(CreateOrganizationInput createOrganizationInput)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(createOrganizationInput));
            var organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            return new OrganizationHashAddressPair
            {
                OrganizationAddress = organizationAddress,
                OrganizationHash = organizationHash
            };
        }

        private void AssertProposalNotYetVotedBySender(ProposalInfo proposal, Address sender)
        {
            var isAlreadyVoted = proposal.Approvals.Contains(sender) || proposal.Rejections.Contains(sender) ||
                                 proposal.Abstentions.Contains(sender);

            Assert(!isAlreadyVoted, "Sender already voted.");
        }
    }
}