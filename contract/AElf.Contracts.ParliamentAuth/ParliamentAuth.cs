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
            Assert(organization != null, "No registered organization.");
            return organization;
        }
        
        public override ProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Not found proposal.");
            
            var result = new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress
            };

            return result;
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
                CreateOrganization(new CreateOrganizationInput {ReleaseThreshold = _defaultOrganizationReleaseThreshold});
            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            Assert(input.ReleaseThreshold > 0 && input.ReleaseThreshold <= 10000, "Invalid organization.");
            var organizationHash = GenerateOrganizationVirtualHash(input);
            Address organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            if (State.Organisations[organizationAddress] == null)
            {
                var organization = new Organization
                {
                    ReleaseThreshold = input.ReleaseThreshold,
                    OrganizationAddress = organizationAddress,
                    OrganizationHash = organizationHash
                };
                State.Organisations[organizationAddress] = organization;
            }

            return organizationAddress;
        }

        public override Hash CreateProposal(CreateProposalInput proposal)
        {
            CheckProposerAuthority(proposal.OrganizationAddress);
            var organization = State.Organisations[proposal.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            Assert(
                !string.IsNullOrWhiteSpace(proposal.ContractMethodName)
                && proposal.ToAddress != null
                && proposal.OrganizationAddress != null
                && proposal.ExpiredTime != null, "Invalid proposal.");
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime, "Expired proposal.");
            Hash hash = Hash.FromMessage(proposal);
            Assert(State.Proposals[hash] == null, "Proposal already exists.");
            State.Proposals[hash] = new ProposalInfo
            {
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                Params = proposal.Params,
                ToAddress = proposal.ToAddress,
                OrganizationAddress = proposal.OrganizationAddress,
                ProposalId = hash,
                Proposer = Context.Sender
            };
            return Hash.FromMessage(proposal);
        }

        public override BoolValue Approve(ApproveInput approvalInput)
        {
            var proposalInfo = State.Proposals[approvalInput.ProposalId];
            Assert(proposalInfo != null, "Not found proposal.");
            if (Context.CurrentBlockTime > proposalInfo.ExpiredTime)
            {
                // expired proposal
                // TODO: Set null to delete data from state db.
                //State.Proposals[approvalInput.ProposalId] = null;
                return new BoolValue{Value = false};
            }
            // check approval not existed
            Assert(!proposalInfo.ApprovedRepresentatives.Contains(Context.Sender),
                "Approval already existed.");
            var representatives = GetRepresentatives();
            Assert(IsValidRepresentative(representatives), "Not authorized approval.");
            proposalInfo.ApprovedRepresentatives.Add(Context.Sender);
            State.Proposals[approvalInput.ProposalId] = proposalInfo;
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            if (IsReadyToRelease(proposalInfo, organization, representatives))
            {
                Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress, proposalInfo.ContractMethodName,
                    proposalInfo.Params);
                //State.Proposals[approvalInput.ProposalId] = null;
            }
            return new BoolValue{Value = true};
        }
    }
}