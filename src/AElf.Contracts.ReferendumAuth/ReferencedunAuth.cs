using Acs3;
using AElf.Contracts.MultiToken.Messages;
using Google.Protobuf.WellKnownTypes;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferendumAuthContract : ReferendumAuthContractContainer.ReferendumAuthContractBase
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
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);
            var organization = State.Organisations[proposal.OrganizationAddress];
            var result = new ProposalOutput
            {
                ProposalHash = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                CanBeReleased = Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime() &&
                                !State.ProposalReleaseStatus[proposalId].Value &&
                                IsReadyToRelease(proposalId, organization.OrganizationAddress)
            };

            return result;
        }

        #endregion
        
        public override Empty Initialize(ReferendumAuthContractInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
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
                var organization = new Organization
                {
                    ReleaseThreshold = input.ReleaseThreshold,
                    OrganizationAddress = organizationAddress,
                    TokenSymbol = input.TokenSymbol,
                    OrganizationHash = organizationHash
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
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(approval.ProposalHash);
            var organization = GetOrganization(proposal.OrganizationAddress);

            var lockedVoteAmount = Int64Value.Parser.ParseFrom(approval.InputData).Value;
            
            Assert(State.LockedVoteAmount[Context.Sender][approval.ProposalHash] == null, "Cannot approve more than once.");
            State.LockedVoteAmount[Context.Sender][approval.ProposalHash] = new VoteInfo
                {Amount = lockedVoteAmount, LockId = Context.TransactionId};
            State.ApprovedVoteAmount[approval.ProposalHash].Value += lockedVoteAmount;
            
            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = organization.TokenSymbol,
                Amount = lockedVoteAmount,
                LockId = Context.TransactionId,
                Usage = "Referendum."
            });
            return new BoolValue{Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
            Assert(!State.ProposalReleaseStatus[proposalId].Value, "Proposal already released");
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);
            Assert(IsReadyToRelease(proposalId, proposal.OrganizationAddress), "Not authorized to release.");

            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),"Expired proposal.");
            var organization = GetOrganization(proposal.OrganizationAddress);
            var virtualHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), organization.OrganizationHash);
            Context.SendVirtualInline(virtualHash, proposal.ToAddress, proposal.ContractMethodName, proposal.Params);
            State.ProposalReleaseStatus[proposalId] = new BoolValue{Value = true};

            return new Empty();
        }

        public override Empty ReclaimVote(Hash proposalId)
        {
            ValidateProposalContract();
            var proposal = State.ProposalContract.GetProposal.Call(proposalId);
            var vote = State.LockedVoteAmount[Context.Sender][proposalId];
            Assert(vote != null, "Nothing to reclaim.");
            Assert(State.ProposalReleaseStatus[proposalId].Value || Context.CurrentBlockTime > proposal.ExpiredTime.ToDateTime(),
                "Unable to reclaim at this time.");
            
            var organization = GetOrganization(proposal.OrganizationAddress);
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Amount = vote.Amount,
                From = Context.Sender,
                LockId = vote.LockId,
                Symbol = organization.TokenSymbol,
                To = Context.Self,
                Usage = "Referendum."
            });
            return new Empty();
        }
    }
}