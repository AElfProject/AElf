
using System;
using System.Linq;
using AElf.Contracts.ProposalContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Proposal
{
    public class ProposalContract : ProposalContractContainer.ProposalContractBase
    {
        public override Hash CreateProposal(CreateProposalInput proposal)
        {
            // check validity of proposal
            Assert(
                proposal.ProposalId != null
                && !string.IsNullOrEmpty(proposal.ContractMethodName)
                && proposal.ToAddress != null
                && proposal.Proposer != null
                && proposal.ExpiredTime != null, "Invalid proposal.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();

            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");

            var existing = State.Proposals[proposal.ProposalId];
            Assert(existing == null, "Proposal already created.");

            State.Proposals[proposal.ProposalId] = new ProposalInfo
            {
                ProposalHash = proposal.ProposalId,
                ContractMethodName = proposal.ContractMethodName,
                ToAddress = proposal.ToAddress,
                ExpiredTime = proposal.ExpiredTime,
                Params = proposal.Params,
                Sender = Context.Sender,
                Proposer = proposal.Proposer
            };
            return proposal.ProposalId;
        }

        public override BoolValue Approve(Approval approval)
        {
            // check validity of proposal 
            Hash hash = approval.ProposalHash;

            var approved = State.Approved[hash];
            // check approval not existed
            Assert(approved == null || !approved.Approvals.Contains(approval),
                "Approval already existed.");

            var proposalInfo = State.Proposals[hash];
            // check authorization and permission 
            Assert(proposalInfo != null, "Proposal not found.");
            Assert(Context.CurrentBlockTime < proposalInfo.ExpiredTime.ToDateTime(), 
                "Expired proposal.");
            Assert(Context.Sender.Equals(proposalInfo.Sender), "Incorrect sender address.");
            approved = approved ?? new ApprovedResult();
            approved.Approvals.Add(approval);
            State.Approved[hash] = approved;

            return new BoolValue {Value = true};
        }

        public override GetProposalOutput GetProposal(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Not found proposal.");

            var result = new GetProposalOutput
            {
                Proposer = proposalInfo.Proposer,
                ContractMethodName = proposalInfo.ContractMethodName,
                Params = proposalInfo.Params,
                ProposalHash = proposalInfo.ProposalHash,
                ToAddress = proposalInfo.ToAddress,
                ExpiredTime = proposalInfo.ExpiredTime
            };

            return result;
        }

        public override ApprovedResult GetApprovedResult(Hash proposalId)
        {
            var approvedResult = State.Approved[proposalId];
            Assert(approvedResult != null, "Not found approved result.");
            return approvedResult;
        }
    }
}