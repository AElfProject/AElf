using System.Linq;
using Acs3;
using AElf.Contracts.ProposalContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using CreateProposalInput = Acs3.CreateProposalInput;
using GetProposalOutput = Acs3.GetProposalOutput;

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
        
        public override GetProposalOutput GetProposal(Hash proposalId)
        {
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);
            var organization = State.Organisations[proposal.OrganizationAddress];
            var result = new GetProposalOutput
            {
                ProposalHash = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                CanBeReleased = Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime() &&
                                !State.ProposalReleaseStatus[proposalId].Value &&
                                CheckApprovals(proposalId, organization.ReleaseThreshold)
            };

            return result;
        }

        #endregion view
        public override Empty Initialize(ParliamentAuthInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ConsensusContractSystemName.Value = input.ConsensusContractSystemName;
            State.ProposalContractSystemName.Value = input.ProposalContractSystemName;
            State.Initialized.Value = true;
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
                    ReleaseThreshold = input.ReleaseThreshold,
                    OrganizationAddress = organizationAddress
                };
                State.Organisations[organizationAddress] = organization;
            }
            return organizationAddress;
        }
        
        public override Hash CreateProposal(CreateProposalInput proposal)
        {
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
            var representatives = GetRepresentatives();
            Assert(representatives.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
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
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);
            var organization = State.Organisations[proposal.OrganizationAddress];
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),
                "Expired proposal.");
            Assert(!State.ProposalReleaseStatus[proposalId].Value, "Proposal already released");

            // check approvals
            Assert(CheckApprovals(proposalId, organization.ReleaseThreshold), "Not authorized to release.");
            
            Context.SendInline(proposal.ToAddress, proposal.ContractMethodName, proposal.Params);
            
            State.ProposalReleaseStatus[proposalId] = new BoolValue{Value = true};
            return new Empty();
        }
    }
}