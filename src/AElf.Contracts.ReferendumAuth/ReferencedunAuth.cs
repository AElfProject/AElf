using System;
using System.Linq;
using Acs3;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferencedunAuthContract : ReferendumAuthContractContainer.ReferendumAuthContractBase
    {
        public override Empty Initialize(ReferendumAuthContractInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.ParliamentAuthContractSystemName.Value = input.ParliamentAuthContractSystemName;
            State.AssociationAuthContractSystemName.Value = input.AssociationAuthContractSystemName;
            State.TokenContractAddressSystemName.Value = input.TokenContractSystemName;
            return new Empty();
        }

        public override Hash CreateProposal(Proposal proposal)
        {
            Assert(State.Initialized.Value, "Contract not initialized.");
            CheckParliamentAddress();
            
            // check validity of proposal
            Assert(
                proposal.Name != null
                //&& proposal.MultiSigAccount != null
                && proposal.ToAddress != null
                && proposal.Params != null
                && proposal.Proposer == Context.Sender, "Invalid proposal.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();

            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");

            Hash hash = Hash.FromMessage(proposal);
            var existing = State.Proposals[hash];
            Assert(existing == null, "Proposal already created.");

            State.Proposals[hash] = new ProposalInfo
            {
                ProposalHash = hash,
                Proposal = proposal,
                IsReleased = false
            };
            return hash;
        }

        public override BoolValue SayYes(Approval approval)
        {
            // check validity of proposal 
            Hash hash = approval.ProposalHash;

            var proposalInfo = State.Proposals[hash];
            // check authorization and permission 
            Assert(proposalInfo != null, "Proposal not found.");
            var proposal = proposalInfo.Proposal;
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");

            var lockedVoteAmount = Int64Value.Parser.ParseFrom(approval.InputData).Value;
            
            Assert(State.LockedVoteAmount[Context.Sender][hash] == null, "Cannot approve more than once.");
            State.LockedVoteAmount[Context.Sender][hash] = new VoteInfo
                {Amount = lockedVoteAmount, LockId = Context.TransactionId};
            
            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = ReferendumConsts.VoteTokenInfoName,
                Amount = lockedVoteAmount,
                LockId = Context.TransactionId,
                Usage = "Referendum."
            });
            return new BoolValue{Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            Assert(IsReadyToRelease(proposalInfo), "Not authorized to release.");

            // check expired time of proposal
            var proposal = proposalInfo.Proposal;
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),"Expired proposal.");
            Assert(!proposalInfo.IsReleased, "Proposal already released");
            // check approvals
           
            Context.SendInline(proposal.ToAddress, proposal.Name, proposal.Params);
            proposalInfo.IsReleased = true;
            State.Proposals[proposalId] = proposalInfo;
            return new Empty();
        }

        public override Empty ReclaimVote(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Not found proposal.");
            var vote = State.LockedVoteAmount[Context.Sender][proposalId];
            Assert(vote != null, "Nothing to reclaim.");
            Assert(proposalInfo.IsReleased || Context.CurrentBlockTime > proposalInfo.Proposal.ExpiredTime.ToDateTime(),
                "Unable to reclaim at this time.");
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Amount = vote.Amount,
                From = Context.Sender,
                LockId = vote.LockId,
                Symbol = ReferendumConsts.VoteTokenInfoName,
                To = Context.Self,
                Usage = "Referendum."
            });
            return new Empty();
        }

        public override GetProposalOutput GetProposal(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Not found proposal.");

            var proposal = proposalInfo.Proposal;
            var result = new GetProposalOutput
            {
                Proposal = proposal,
                CanBeReleased = false
            };

            if (proposalInfo.IsReleased)
            {
                result.Status = ProposalStatus.Released;
            }
            else if (Context.CurrentBlockTime > proposalInfo.Proposal.ExpiredTime.ToDateTime())
            {
                result.Status = ProposalStatus.Expired;
            }
            else
            {
                result.Status = ProposalStatus.Active;
                result.CanBeReleased = IsReadyToRelease(proposalInfo);
            }

            return result;
        }
    }
}