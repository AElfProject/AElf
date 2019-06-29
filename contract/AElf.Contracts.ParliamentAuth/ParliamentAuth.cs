using System;
using Acs3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using CreateProposalInput = Acs3.CreateProposalInput;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract : ParliamentAuthContractContainer.ParliamentAuthContractBase
    {
        #region View

        public override Organization GetOrganization(Address address)
        {
            var organization = State.Organisations[address];
            return organization ?? new Organization();
        }

        public override ProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null)
            {
                return new ProposalOutput();
            }

            return new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress
            };
        }

        public override Address GetDefaultOrganizationAddress(Empty input)
        {
            Assert(State.Initialized.Value, "Not initialized.");
            return State.DefaultOrganizationAddress.Value;
        }

        #endregion view

        public override Empty Initialize(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            State.DefaultOrganizationAddress.Value =
                CreateOrganization(
                    new CreateOrganizationInput {ReleaseThreshold = DefaultReleaseThreshold});
            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            var organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            var organization = new Organization
            {
                ReleaseThreshold = input.ReleaseThreshold,
                OrganizationAddress = organizationAddress,
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
            var organization = State.Organisations[input.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            AssertSenderIsAuthorizedProposer(organization);

            Hash hash = Hash.FromMessage(input);
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
            return hash;
        }

        public override BoolValue Approve(ApproveInput approvalInput)
        {
            var proposal = GetValidProposal(approvalInput.ProposalId);
            AssertProposalNotYetApprovedBySender(proposal);
            var currentParliament = GetCurrentMinerList();
            AssertSenderIsParliementMember(currentParliament);

            proposal.ApprovedRepresentatives.Add(Context.Sender);
            State.Proposals[approvalInput.ProposalId] = proposal;

            // organization stores the release threshold
            var organization = State.Organisations[proposal.OrganizationAddress];
            if (IsReleaseThresholdReached(proposal, organization, currentParliament))
            {
                Context.SendVirtualInline(
                    organization.OrganizationHash,
                    proposal.ToAddress,
                    proposal.ContractMethodName,
                    proposal.Params);
                //State.Proposals[approvalInput.ProposalId] = null;
            }

            return new BoolValue {Value = true};
        }
    }
}