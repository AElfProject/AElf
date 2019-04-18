using System;
using System.Linq;
using Acs3;
using AElf.Contracts.ProposalContract;
using Google.Protobuf;
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
            var organization = State.Organisations[proposal.OrganizationAddress];
            var representatives = GetRepresentatives();
            var approved = State.Approved[proposalId];

            var result = new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                CanBeReleased = Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime() &&
                                CheckApprovals(approved, organization, representatives)
            };

            return result;
        }

        #endregion view
        public override Empty Initialize(ParliamentAuthInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ConsensusContractSystemName.Value = input.ConsensusContractSystemName;
            State.Initialized.Value = true;
            return new Empty();
        }
        
        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromMessage(input);
            Address organizationAddress =
                Context.ConvertVirtualAddressToContractAddress(Hash.FromTwoHashes(Hash.FromMessage(Context.Self),
                    organizationHash));
            if(State.Organisations[organizationAddress] == null)
            {
                var organization =new Organization
                {
                    ReleaseThresholdInFraction = input.ReleaseThresholdInFraction,
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
            Assert(
                !string.IsNullOrWhiteSpace(proposal.ContractMethodName)
                && proposal.ToAddress != null
                && proposal.OrganizationAddress != null
                && proposal.ExpiredTime != null, "Invalid proposal.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();
            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");
            Hash hash = Hash.FromMessage(proposal);
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

        public override Empty Approve(ApproveInput approvalInput)
        {
            var proposalInfo = State.Proposals[approvalInput.ProposalId];
            Assert(proposalInfo != null, "Not found proposal.");
            var approved = State.Approved[approvalInput.ProposalId];
            // check approval not existed
            Assert(approved == null || !approved.ApprovedRepresentatives.Contains(Context.Sender),
                "Approval already existed.");
            var representatives = GetRepresentatives();
            Assert(representatives.Any(r => r.Equals(Context.Sender)), "Not authorized approval.");
            approved.ApprovedRepresentatives.Add(Context.Sender);
            State.Approved[approvalInput.ProposalId] = approved;
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            if (CheckApprovals(approved, organization, representatives))
            {
                var virtualHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), organization.OrganizationHash);
                Context.SendVirtualInline(virtualHash, proposalInfo.ToAddress, proposalInfo.ContractMethodName,
                    proposalInfo.Params);
                State.Proposals[approvalInput.ProposalId] = null;
                State.Approved[approvalInput.ProposalId] = null;
            }
            return new Empty();
        }
    }
}