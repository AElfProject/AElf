using System.Linq;
using Acs3;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Referendum
{
    public partial class ReferendumContract : ReferendumContractContainer.ReferendumContractBase
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
                ApprovalCount = proposal.AbstentionCount,
                RejectionCount = proposal.RejectionCount,
                AbstentionCount = proposal.AbstentionCount
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
        
        #endregion

        public override Empty Initialize(Empty input)
        {
            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
            var organizationAddress = organizationHashAddressPair.OrganizationAddress;
            var organizationHash = organizationHashAddressPair.OrganizationHash;
            Assert(State.Organisations[organizationAddress] == null, "Organization already exists.");
            var organization = new Organization
            {
                ProposalReleaseThreshold = input.ProposalReleaseThreshold,
                OrganizationAddress = organizationAddress,
                TokenSymbol = input.TokenSymbol,
                OrganizationHash = organizationHash,
                ProposerWhiteList = input.ProposerWhiteList
            };
            Assert(Validate(organization), "Invalid organization data.");

            State.Organisations[organizationAddress] = organization;
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
            var organization = State.Organisations[proposal.OrganizationAddress];
            var allowance = GetAllowance(Context.Sender, organization.TokenSymbol);

            proposal.ApprovalCount = proposal.ApprovalCount.Add(allowance);
            State.Proposals[input] = proposal;
            LockToken(organization.TokenSymbol, allowance, input, Context.Sender);
            return new Empty();
        }

        public override Empty Reject(Hash input)
        {
            var proposal = GetValidProposal(input);
            var organization = State.Organisations[proposal.OrganizationAddress];
            var allowance = GetAllowance(Context.Sender, organization.TokenSymbol);

            proposal.RejectionCount = proposal.RejectionCount.Add(allowance);
            State.Proposals[input] = proposal;
            LockToken(organization.TokenSymbol, allowance, input, Context.Sender);
            return new Empty();
        }

        public override Empty Abstain(Hash input)
        {
            var proposal = GetValidProposal(input);
            var organization = State.Organisations[proposal.OrganizationAddress];
            var allowance = GetAllowance(Context.Sender, organization.TokenSymbol);

            proposal.AbstentionCount = proposal.AbstentionCount.Add(allowance);
            State.Proposals[input] = proposal;
            LockToken(organization.TokenSymbol, allowance, input, Context.Sender);
            return new Empty();
        }

        public override Empty ReclaimVoteToken(Hash input)
        {
            var proposal = State.Proposals[input];
            Assert(proposal == null ||
                   Context.CurrentBlockTime > proposal.ExpiredTime, "Unable to reclaim at this time.");
            UnlockToken(input, Context.Sender);
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

        public override Empty Release(Hash input)
        {
            var proposal = GetValidProposal(input);
            Assert(Context.Sender.Equals(proposal.Proposer), "No permission.");
            var organization = State.Organisations[proposal.OrganizationAddress];
            Assert(IsReleaseThresholdReached(proposal, organization), "Not approved.");
            Context.SendVirtualInlineBySystemContract(organization.OrganizationHash, proposal.ToAddress,
                proposal.ContractMethodName, proposal.Params);

            Context.Fire(new ProposalReleased {ProposalId = input});
            State.Proposals.Remove(input);

            return new Empty();
        }
    }
}