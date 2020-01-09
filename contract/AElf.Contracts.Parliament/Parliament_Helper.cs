using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Parliament
{
    public partial class ParliamentContract
    {
        private List<Address> GetCurrentMinerList()
        {
            MaybeLoadConsensusContractAddress();
            var miner = State.ConsensusContract.GetCurrentMinerList.Call(new Empty());
            var members = miner.Pubkeys.Select(publicKey =>
                Address.FromPublicKey(publicKey.ToByteArray())).ToList();
            return members;
        }

        private void AssertIsAuthorizedProposer(Address organizationAddress, Address proposer)
        {
            var organization = State.Organisations[organizationAddress];
            Assert(organization != null, "No registered organization.");
            // It is a valid proposer if
            // authority check is disable,
            // or sender is in proposer white list,
            // or sender is one of miners when member proposing allowed.
            Assert(
                !organization.ProposerAuthorityRequired || ValidateAddressInWhiteList(proposer) ||
                organization.ParliamentMemberProposingAllowed && ValidateParliamentMemberAuthority(proposer),
                "Unauthorized to propose.");
        }

        private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization)
        {
            var parliamentMembers = GetCurrentMinerList();
            var isRejected = IsProposalRejected(proposal, organization, parliamentMembers);
            if (isRejected)
                return false;

            var isAbstained = IsProposalAbstained(proposal, organization, parliamentMembers);
            if (isAbstained)
                return false;

            return CheckEnoughVoteAndApprovals(proposal, organization, parliamentMembers);
        }

        private bool IsProposalStillPending(ProposalInfo proposal, Organization organization,
            ICollection<Address> parliamentMembers)
        {
            var isRejected = IsProposalRejected(proposal, organization, parliamentMembers);
            if (isRejected)
                return false;

            var isAbstained = IsProposalAbstained(proposal, organization, parliamentMembers);
            if (isAbstained)
                return false;

            return !CheckEnoughVoteAndApprovals(proposal, organization, parliamentMembers);
        }

        private bool IsProposalRejected(ProposalInfo proposal, Organization organization,
            ICollection<Address> parliamentMembers)
        {
            var rejectionMemberCount = proposal.Rejections.Count(parliamentMembers.Contains);
            return rejectionMemberCount * AbstractVoteTotal >
                   organization.ProposalReleaseThreshold.MaximalRejectionThreshold * parliamentMembers.Count;
        }

        private bool IsProposalAbstained(ProposalInfo proposal, Organization organization,
            ICollection<Address> parliamentMembers)
        {
            var abstentionMemberCount = proposal.Abstentions.Count(parliamentMembers.Contains);
            return abstentionMemberCount * AbstractVoteTotal >
                   organization.ProposalReleaseThreshold.MaximalAbstentionThreshold * parliamentMembers.Count;
        }

        private bool CheckEnoughVoteAndApprovals(ProposalInfo proposal, Organization organization,
            ICollection<Address> parliamentMembers)
        {
            var approvedMemberCount = proposal.Approvals.Count(parliamentMembers.Contains);
            var isApprovalEnough = approvedMemberCount * AbstractVoteTotal >=
                                   organization.ProposalReleaseThreshold.MinimalApprovalThreshold *
                                   parliamentMembers.Count;
            if (!isApprovalEnough)
                return false;

            var isVoteThresholdReached = IsVoteThresholdReached(proposal, organization, parliamentMembers);
            return isVoteThresholdReached;
        }

        private bool IsVoteThresholdReached(ProposalInfo proposal, Organization organization,
            ICollection<Address> parliamentMembers)
        {
            var isVoteThresholdReached =
                proposal.Abstentions.Concat(proposal.Approvals).Concat(proposal.Rejections)
                    .Count(parliamentMembers.Contains) * AbstractVoteTotal >=
                organization.ProposalReleaseThreshold.MinimalVoteThreshold * parliamentMembers.Count;
            return isVoteThresholdReached;
        }

        private void MaybeLoadConsensusContractAddress()
        {
            if (State.ConsensusContract.Value != null)
                return;
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        }

        private void AssertSenderIsParliamentMember()
        {
            var currentParliament = GetCurrentMinerList();
            Assert(CheckSenderIsParliamentMember(currentParliament), "Unauthorized member.");
        }

        private bool CheckSenderIsParliamentMember(IEnumerable<Address> currentParliament)
        {
            return currentParliament.Any(r => r.Equals(Context.Sender));
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
            var validExpiredTime = CheckProposalNotExpired(proposal);
            var hasOrganizationAddress = proposal.OrganizationAddress != null;
            return validDestinationAddress && validDestinationMethodName && validExpiredTime & hasOrganizationAddress;
        }

        private bool CheckProposalNotExpired(ProposalInfo proposal)
        {
            return proposal.ExpiredTime != null && Context.CurrentBlockTime < proposal.ExpiredTime;
        }

        private ProposalInfo GetValidProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Proposal not found.");
            Assert(Validate(proposal), "Invalid proposal.");
            return proposal;
        }

        private void AssertProposalNotYetVotedBySender(ProposalInfo proposal)
        {
            Assert(!CheckSenderAlreadyVoted(proposal, Context.Sender), "Already approved.");
        }

        private bool CheckSenderAlreadyVoted(ProposalInfo proposal, Address address)
        {
            return proposal.Approvals.Contains(address) || proposal.Rejections.Contains(address) ||
                   proposal.Abstentions.Contains(address);
        }

        private bool ValidateAddressInWhiteList(Address address)
        {
            return State.ProposerWhiteList.Value.Proposers.Any(p => p == address);
        }

        private bool ValidateParliamentMemberAuthority(Address address)
        {
            var currentMinerList = GetCurrentMinerList();
            return currentMinerList.Any(m => m == address);
        }

        private void AssertCurrentMiner()
        {
            MaybeLoadConsensusContractAddress();
            var isCurrentMiner = State.ConsensusContract.IsCurrentMiner.Call(Context.Sender).Value;
            Context.LogDebug(() => $"Sender is currentMiner : {isCurrentMiner}.");
            Assert(isCurrentMiner, "No permission.");
        }

        private Hash CreateNewProposal(CreateProposalInput input)
        {
            Hash proposalId = Hash.FromTwoHashes(Hash.FromTwoHashes(Hash.FromMessage(input), Context.TransactionId),
                Hash.FromRawBytes(Context.CurrentBlockTime.ToByteArray()));
            var proposal = new ProposalInfo
            {
                ContractMethodName = input.ContractMethodName,
                ExpiredTime = input.ExpiredTime,
                Params = input.Params,
                ToAddress = input.ToAddress,
                OrganizationAddress = input.OrganizationAddress,
                ProposalId = proposalId,
                Proposer = Context.Sender
            };
            Assert(Validate(proposal), "Invalid proposal.");
            Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
            State.Proposals[proposalId] = proposal;
            Context.Fire(new ProposalCreated {ProposalId = proposalId});
            return proposalId;
        }

        private Address CreateNewOrganization(CreateOrganizationInput input)
        {
            var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
            var organizationAddress = organizationHashAddressPair.OrganizationAddress;
            var organizationHash = organizationHashAddressPair.OrganizationHash;
            var organization = new Organization
            {
                ProposalReleaseThreshold = input.ProposalReleaseThreshold,
                OrganizationAddress = organizationAddress,
                OrganizationHash = organizationHash,
                ProposerAuthorityRequired = input.ProposerAuthorityRequired,
                ParliamentMemberProposingAllowed = input.ParliamentMemberProposingAllowed
            };
            Assert(Validate(organization), "Invalid organization.");
            if (State.Organisations[organizationAddress] == null)
            {
                State.Organisations[organizationAddress] = organization;
            }

            Context.Fire(new OrganizationCreated
            {
                OrganizationAddress = organizationAddress
            });

            return organizationAddress;
        }

        private OrganizationHashAddressPair CalculateOrganizationHashAddressPair(
            CreateOrganizationInput createOrganizationInput)
        {
            var organizationHash = Hash.FromMessage(createOrganizationInput);
            var organizationAddress =
                Context.ConvertVirtualAddressToContractAddressWithContractHashName(organizationHash);
            return new OrganizationHashAddressPair
            {
                OrganizationAddress = organizationAddress,
                OrganizationHash = organizationHash
            };
        }
    }
}