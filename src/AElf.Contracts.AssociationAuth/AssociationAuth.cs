using System;
using System.Linq;
using Acs3;
using Google.Protobuf.WellKnownTypes;
using CreateProposalInput = Acs3.CreateProposalInput;

namespace AElf.Contracts.AssociationAuth
{
    public partial class AssociationAuthContract : AssociationAuthContractContainer.AssociationAuthContractBase
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
            var approved = State.Approved[proposalId];
            var organization = GetOrganization(proposal.OrganizationAddress);

            var result = new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                CanBeReleased = Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime() && CheckApprovals(approved, organization)
            };

            return result;
        }

        #endregion view

        #region Actions

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
                    ReleaseThreshold = input.ReleaseThreshold,
                    OrganizationAddress = organizationAddress,
                    ProposerThreshold = input.ProposerThreshold,
                    OrganizationHash = organizationHash
                };
                organization.Reviewers.AddRange(input.Reviewers);
                State.Organisations[organizationAddress] = organization;
            }
            return organizationAddress;
        }


        public override Hash CreateProposal(CreateProposalInput proposal)
        {
            // check authorization of proposer public key
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
            
            return hash;
        }
    
        public override Empty Approve(ApproveInput approvalInput)
        {
            var proposalInfo = State.Proposals[approvalInput.ProposalId];
            Assert(proposalInfo != null, "Not found proposal.");
            var approved = State.Approved[approvalInput.ProposalId];
            // check approval not existed
            Assert(approved == null || !approved.ApprovedReviewer.Contains(Context.Sender), "Approval already existed.");
            var organization = GetOrganization(proposalInfo.OrganizationAddress);
            var reviewer = organization.Reviewers.FirstOrDefault(r => r.Address.Equals(Context.Sender));
            Assert(reviewer != null,"Not authorized approval.");
            approved.ApprovedReviewer.Add(Context.Sender);
            approved.ApprovedWeight += reviewer.Weight;
            State.Approved[approvalInput.ProposalId] = approved;

            if (CheckApprovals(approved, organization))
            {
                var virtualHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), organization.OrganizationHash);
                Context.SendVirtualInline(virtualHash, proposalInfo.ToAddress, proposalInfo.ContractMethodName,
                    proposalInfo.Params);
                State.Proposals[approvalInput.ProposalId] = null;
                State.Approved[approvalInput.ProposalId] = null;
            }
            return new Empty();
        }

        #endregion
    }
}