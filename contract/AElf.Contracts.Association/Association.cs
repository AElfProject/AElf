using System.Linq;
using Acs3;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using CreateProposalInput = Acs3.CreateProposalInput;

namespace AElf.Contracts.Association
{
    public partial class AssociationContract : AssociationContractContainer.AssociationContractBase
    {
        #region View

        public override Organization GetOrganization(Address address)
        {
            return State.Organisations[address] ?? new Organization();
        }

        public override ProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null)
            {
                return new ProposalOutput();
            }
            
            var organization = State.Organisations[proposal.OrganizationAddress];
            var readyToRelease = IsReleaseThresholdReached(proposal, organization);

            return new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress,
                ToBeReleased = readyToRelease,
                ApprovalCount = proposal.Approvals.Count,
                RejectionCount = proposal.Rejections.Count,
                AbstentionCount = proposal.Abstentions.Count
            };
        }

        public override Address CalculateOrganizationAddress(CreateOrganizationInput input)
        {
            var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
            var organizationAddress = organizationHashAddressPair.OrganizationAddress;
            return organizationAddress;
        }

        public override BoolValue ValidateOrganizationExist(Address input)
        {
            return new BoolValue {Value = State.Organisations[input] != null};
        }

        public override BoolValue ValidateProposerInWhiteList(ValidateProposerInWhiteListInput input)
        {
            var organization = State.Organisations[input.OrganizationAddress];
            return new BoolValue
            {
                Value = organization.ProposerWhiteList.Contains(input.Proposer)
            };
        }

        #endregion view

        #region Actions

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
            var organizationAddress = organizationHashAddressPair.OrganizationAddress;
            var organizationHash = organizationHashAddressPair.OrganizationHash;
            var organization = new Organization
            {
                ProposalReleaseThreshold = input.ProposalReleaseThreshold,
                OrganizationAddress = organizationAddress,
                ProposerWhiteList = input.ProposerWhiteList,
                OrganizationMemberList = input.OrganizationMemberList,
                OrganizationHash = organizationHash
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


        public override Hash CreateProposal(CreateProposalInput input)
        {
            AssertIsAuthorizedProposer(input.OrganizationAddress, Context.Sender);
            var proposalId = CreateNewProposal(input);
            return proposalId;
        }

        public override Hash CreateProposalBySystemContract(CreateProposalBySystemContractInput input)
        {
            Assert(Context.GetSystemContractNameToAddressMapping().Values.Contains(Context.Sender),
                "Not authorized to propose.");
            AssertIsAuthorizedProposer(input.ProposalInput.OrganizationAddress, input.OriginProposer);
            var proposalId = CreateNewProposal(input.ProposalInput);
            if (!string.IsNullOrEmpty(input.ProposalIdFeedbackMethod))
                Context.SendInline(Context.Sender, input.ProposalIdFeedbackMethod, proposalId); // proposal id feedback
            return proposalId;
        }

        public override Empty Approve(Hash input)
        {
            var proposal = GetValidProposal(input);
            AssertProposalNotYetVotedBySender(proposal, Context.Sender);
            var organization = GetOrganization(proposal.OrganizationAddress);
            AssertIsAuthorizedOrganizationMember(organization, Context.Sender);

            proposal.Approvals.Add(Context.Sender);
            State.Proposals[input] = proposal;
            return new Empty();
        }

        public override Empty Reject(Hash input)
        {
            var proposal = GetValidProposal(input);
            AssertProposalNotYetVotedBySender(proposal, Context.Sender);
            var organization = GetOrganization(proposal.OrganizationAddress);
            AssertIsAuthorizedOrganizationMember(organization, Context.Sender);

            proposal.Rejections.Add(Context.Sender);
            State.Proposals[input] = proposal;
            return new Empty();
        }

        public override Empty Abstain(Hash input)
        {
            var proposal = GetValidProposal(input);
            AssertProposalNotYetVotedBySender(proposal, Context.Sender);
            var organization = GetOrganization(proposal.OrganizationAddress);
            AssertIsAuthorizedOrganizationMember(organization, Context.Sender);

            proposal.Abstentions.Add(Context.Sender);
            State.Proposals[input] = proposal;
            return new Empty();
        }

        public override Empty Release(Hash input)
        {
            var proposalInfo = GetValidProposal(input);
            Assert(Context.Sender == proposalInfo.Proposer, "No permission.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            Assert(IsReleaseThresholdReached(proposalInfo, organization), "Not approved.");
            Context.SendVirtualInlineBySystemContract(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);

            Context.Fire(new ProposalReleased {ProposalId = input});
            State.Proposals.Remove(input);
            
            return new Empty();
        }

        public override Empty ChangeOrganizationThreshold(ProposalReleaseThreshold input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.ProposalReleaseThreshold = input;
            Assert(Validate(organization), "Invalid organization.");
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }

        public override Empty ChangeOrganizationMember(OrganizationMemberList input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.OrganizationMemberList = input;
            Assert(Validate(organization), "Invalid organization.");
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }

        public override Empty ChangeOrganizationProposerWhiteList(ProposerWhiteList input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.ProposerWhiteList = input;
            Assert(Validate(organization), "Invalid organization.");
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }
        
        public override Empty ClearProposal(Hash input)
        {
            // anyone can clear proposal if it is expired
            var proposal = State.Proposals[input];
            Assert(proposal != null && Context.CurrentBlockTime > proposal.ExpiredTime, "Proposal clear failed");
            State.Proposals.Remove(input);
            return new Empty();
        }

        #endregion
    }
}