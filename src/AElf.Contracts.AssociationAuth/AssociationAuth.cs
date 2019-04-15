using System;
using System.Linq;
using Acs3;
using AElf.Contracts.ProposalContract;
using Google.Protobuf;
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
        
        public override GetProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);

            var result = new GetProposalOutput
            {
                ProposalHash = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                CanBeReleased = Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime() &&
                                !State.ProposalReleaseStatus[proposalId].Value && CheckApprovals(proposalId)
            };

            return result;
        }

        #endregion view

        #region Actions

        public override Empty Initialize(AssociationAuthContractInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ProposalContractSystemName.Value = input.ProposalContractSystemName;
            State.Initialized.Value = true;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            Address organizationAddress =
                Context.ConvertVirtualAddressToContractAddress(Hash.FromTwoHashes(Hash.FromMessage(Context.Self),
                    Hash.FromMessage(input)));
            if(State.Organisations[organizationAddress] == null)
            {
                var organization =new Organization
                {
                    ExecutionThreshold = input.ExecutionThreshold,
                    OrganizationAddress = organizationAddress
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
            ValidateProposalContract();
            State.ProposalContract.CreateProposal.Send(new ProposalContract.CreateProposalInput
            {
                ContractMethodName = proposal.ContractMethodName,
                ToAddress = proposal.ToAddress,
                ExpiredTime = proposal.ExpiredTime,
                Params = proposal.Params,
                OrganizationAddress = proposal.OrganizationAddress,
                Proposer = Context.Sender
            });
            return Hash.FromMessage(proposal);
        }

        public override BoolValue Approve(ApproveInput approval)
        {
            byte[] pubKey = Context.RecoverPublicKey();
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(approval.ProposalHash);
            var organization = GetOrganization(proposal.OrganizationAddress);
            Assert(organization.Reviewers.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
                "Not authorized approval.");
            State.ProposalContract.Approve.Send(new Approval
            {
                ProposalHash = approval.ProposalHash,
                PublicKey = ByteString.CopyFrom(pubKey)
            });

            return new BoolValue {Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
            // check expired time of proposal
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);

            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),
                "Expired proposal.");
            Assert(!State.ProposalReleaseStatus[proposalId].Value, "Proposal already released");

            // check approvals
            Assert(CheckApprovals(proposalId), "Not authorized to release.");
            
            Context.SendInline(proposal.ToAddress, proposal.ContractMethodName, proposal.Params);
            
            State.ProposalReleaseStatus[proposalId] = new BoolValue{Value = true};
            return new Empty();
        }

        #endregion
    }
}