using System;
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
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Proposal not found.");

            var organization = State.Organisations[proposal.OrganizationAddress];
            var result = new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                CanBeReleased = Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime() && 
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
            var organization = State.Organisations[proposal.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            var hash = Hash.FromMessage(proposal);
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
            DateTime timestamp = proposalInfo.ExpiredTime.ToDateTime();
            if (Context.CurrentBlockTime > timestamp)
            {
                // expired proposal
                State.Proposals[approval.ProposalId] = null;
                return new BoolValue{Value = false};
            }
            var lockedVoteAmount = Int64Value.Parser.ParseFrom(approval.InputData).Value;

            Assert(State.LockedVoteAmount[Context.Sender][approval.ProposalId] == null,
                "Cannot approve more than once.");
            var organization = GetOrganization(proposalInfo.OrganizationAddress);
            State.LockedVoteAmount[Context.Sender][approval.ProposalId] = new VoteInfo
            {
                Amount = lockedVoteAmount,
                LockId = Context.TransactionId,
                TokenSymbol = organization.TokenSymbol
            };
            State.ApprovedVoteAmount[approval.ProposalId].Value += lockedVoteAmount;

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = organization.TokenSymbol,
                Amount = lockedVoteAmount,
                LockId = Context.TransactionId,
                Usage = "Referendum."
            });

            if (State.ApprovedVoteAmount[approval.ProposalId].Value >= organization.ReleaseThreshold)
            {
                var virtualHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), organization.OrganizationHash);
                Context.SendVirtualInline(virtualHash, proposalInfo.ToAddress, proposalInfo.ContractMethodName,
                    proposalInfo.Params);
                State.Proposals[approval.ProposalId] = null;
                State.ProposalReleaseStatus[approval.ProposalId] = new BoolValue{Value = true};
            }
            return new BoolValue{Value = true};
        }

        public override Empty ReclaimVote(Hash proposalId)
        {
            var vote = State.LockedVoteAmount[Context.Sender][proposalId];
            Assert(vote != null, "Nothing to reclaim.");
            var proposal = State.Proposals[proposalId];
            Assert(proposal == null ||
                Context.CurrentBlockTime > proposal.ExpiredTime.ToDateTime(), "Unable to reclaim at this time.");
            State.LockedVoteAmount[Context.Sender][proposalId] = null;
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Amount = vote.Amount,
                From = Context.Sender,
                LockId = vote.LockId,
                Symbol = vote.TokenSymbol,
                To = Context.Self,
                Usage = "Referendum."
            });
            return new Empty();
        }
    }
}