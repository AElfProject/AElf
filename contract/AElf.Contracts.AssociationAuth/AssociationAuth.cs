using System;
using System.Linq;
using Acs3;
using AElf.Sdk.CSharp;
using AElf.Types;
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
            Assert(proposal !=null,"Not found proposal.");
            
            var organization = State.Organisations[proposal.OrganizationAddress];
            var readyToRelease = IsReadyToRelease(proposal, organization);
            var result = new ProposalOutput
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

            return result;
        }

        #endregion view

        #region Actions

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var isValidWeight = input.Reviewers.All(r => r.Weight >= 0);
            var canBeProposed = input.Reviewers.Any(r => r.Weight >= input.ProposerThreshold);
            var canBeReleased = input.Reviewers.Aggregate(0, (i, reviewer) => i + reviewer.Weight) >
                                input.ReleaseThreshold;
            Assert(isValidWeight && canBeProposed && canBeReleased, "Invalid organization." );
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            Address organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
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
                && proposal.ExpiredTime != null, "Invalid proposal.");
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime, "Expired proposal.");
            //TODO: Proposals with the same input is not supported. 
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
            
            return hash;
        }
    
        public override BoolValue Approve(ApproveInput approvalInput)
        {
            var proposalInfo = State.Proposals[approvalInput.ProposalId];
            Assert(proposalInfo != null, "Not found proposal.");
            if (Context.CurrentBlockTime > proposalInfo.ExpiredTime)
            {
                // expired proposal
                //State.Proposals[approvalInput.ProposalId] = null;
                return new BoolValue{Value = false};
            }
            
            // check approval not existed
            Assert(!proposalInfo.ApprovedReviewer.Contains(Context.Sender), "Approval already exists.");
            var organization = GetOrganization(proposalInfo.OrganizationAddress);
            var reviewer = organization.Reviewers.FirstOrDefault(r => r.Address.Equals(Context.Sender));
            Assert(reviewer != null,"Not authorized approval.");
            proposalInfo.ApprovedReviewer.Add(Context.Sender);
            proposalInfo.ApprovedWeight += reviewer.Weight;
            State.Proposals[approvalInput.ProposalId] = proposalInfo;

            return new BoolValue {Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            Assert(Context.Sender.Equals(proposalInfo.Proposer), "Unable to release this proposal.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            Assert(IsReadyToRelease(proposalInfo, organization), "Not approved.");
            Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);
            
            return new Empty();
        }

        #endregion
    }
}