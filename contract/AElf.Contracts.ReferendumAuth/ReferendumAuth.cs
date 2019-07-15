using System;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
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
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Proposal not found.");

            var organization = State.Organisations[proposal.OrganizationAddress];
            var readyToRelease = IsReadyToRelease(proposalId, organization);
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

        #endregion
        
        public override Empty Initialize(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            return new Empty();
        }
        
        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            Address organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
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
            Assert(
                !string.IsNullOrWhiteSpace(proposal.ContractMethodName)
                && proposal.ToAddress != null
                && proposal.OrganizationAddress != null
                && proposal.ExpiredTime != null, "Invalid proposal.");
            var organization = State.Organisations[proposal.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime, "Expired proposal.");
            Hash hash = Hash.FromMessage(proposal);
            Assert(State.Proposals[hash] == null, "Proposal already exists.");
            State.Proposals[hash] = new ProposalInfo
            {
                ContractMethodName = proposal.ContractMethodName,
                ToAddress = proposal.ToAddress,
                ExpiredTime = proposal.ExpiredTime,
                Params = proposal.Params,
                OrganizationAddress = proposal.OrganizationAddress,
                Proposer = Context.Sender
            };
            return hash;
        }

        public override BoolValue Approve(ApproveInput approval)
        {
            var proposalInfo = State.Proposals[approval.ProposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            if (Context.CurrentBlockTime > proposalInfo.ExpiredTime)
            {
                // expired proposal
                //State.Proposals[approval.ProposalId] = null;
                return new BoolValue{Value = false};
            }
            Assert(approval.Quantity > 0, "Invalid vote.");
            var lockedTokenAmount = approval.Quantity;

            Assert(State.LockedTokenAmount[Context.Sender][approval.ProposalId] == null,
                "Cannot approve more than once.");
            var organization = GetOrganization(proposalInfo.OrganizationAddress);
            State.LockedTokenAmount[Context.Sender][approval.ProposalId] = new Receipt
            {
                Amount = lockedTokenAmount,
                LockId = Context.TransactionId,
                TokenSymbol = organization.TokenSymbol
            };
            State.ApprovedTokenAmount[approval.ProposalId] += lockedTokenAmount;

            LockToken(new LockInput
            {
                Address = Context.Sender,
                Symbol = organization.TokenSymbol,
                Amount = lockedTokenAmount,
                LockId = Context.TransactionId,
                Usage = "Referendum."
            });
            
            return new BoolValue{Value = true};
        }

        public override Empty ReclaimVoteToken(Hash proposalId)
        {
            var voteToken = State.LockedTokenAmount[Context.Sender][proposalId];
            Assert(voteToken != null, "Nothing to reclaim.");
            var proposal = State.Proposals[proposalId];
            Assert(proposal == null ||
                Context.CurrentBlockTime > proposal.ExpiredTime, "Unable to reclaim at this time.");
            // State.LockedTokenAmount[Context.Sender][proposalId] = null;
            UnlockToken(new UnlockInput
            {
                Amount = voteToken.Amount,
                Address = Context.Sender,
                LockId = voteToken.LockId,
                Symbol = voteToken.TokenSymbol,
                Usage = "Referendum."
            });
            return new Empty();
        }
        
        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            Assert(Context.Sender.Equals(proposalInfo.Proposer), "Unable to release this proposal.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            Assert(IsReadyToRelease(proposalId, organization), "Not approved.");
            Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);
            
            return new Empty();
        }
    }
}