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
                ToBeReleased = readyToRelease
            };
        }

        #endregion view

        #region Actions

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            var organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
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

            return organizationAddress;
        }


        public override Hash CreateProposal(CreateProposalInput input)
        {
            // check authorization of proposer public key
            var organization = State.Organisations[input.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            AssertIsAuthorizedProposer(organization, Context.Sender);
            Hash hash = Hash.FromTwoHashes(Hash.FromMessage(input), Context.TransactionId);
            var proposal = new ProposalInfo
            {
                ContractMethodName = input.ContractMethodName,
                ExpiredTime = input.ExpiredTime,
                Params = input.Params,
                ToAddress = input.ToAddress,
                OrganizationAddress = input.OrganizationAddress,
                ProposalId = hash,
                Proposer = Context.Sender
            };
            Assert(Validate(proposal), "Invalid proposal.");
            Assert(State.Proposals[hash] == null, "Proposal already exists.");
            State.Proposals[hash] = proposal;
            Context.Fire(new ProposalCreated {ProposalId = hash});
            
            return hash;
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

        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            Assert(Context.Sender == proposalInfo.Proposer, "Unable to release this proposal.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            Assert(IsReleaseThresholdReached(proposalInfo, organization), "Not approved.");
            Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);

            Context.Fire(new ProposalReleased {ProposalId = proposalId});
            State.Proposals.Remove(proposalId);
            
            return new Empty();
        }

        public override Empty ChangeOrganizationThreshold(ProposalReleaseThreshold input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.ProposalReleaseThreshold = input;
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }

        public override Empty ChangeOrganizationMember(OrganizationMemberList input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.OrganizationMemberList = input;
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }

        public override Empty ChangeOrganizationProposerWhiteList(ProposerWhiteList input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.ProposerWhiteList = input;
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }

        #endregion
    }
}