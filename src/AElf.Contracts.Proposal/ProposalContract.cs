
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
                proposal.ContractMethodName != null
                && proposal.ToAddress != null
                && proposal.Params != null
                && proposal.Proposer != null, "Invalid proposal.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();

            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");

            Hash hash = Hash.FromMessage(proposal);
            var existing = State.Proposals[hash];
            Assert(existing == null, "Proposal already created.");

            State.Proposals[hash] = new ProposalInfo
            {
                ProposalHash = hash,
                ContractMethodName = proposal.ContractMethodName,
                ToAddress = proposal.ToAddress,
                ExpiredTime = proposal.ExpiredTime,
                Params = proposal.Params,
                Sender = Context.Sender,
                Proposer = proposal.Proposer
            };
            return hash;
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
            byte[] pubKey = Context.RecoverPublicKey();
            Assert(approval.PublicKey.IsEmpty || approval.PublicKey.ToByteArray().SequenceEqual(pubKey),
                "Invalid public key in approval.");
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
    }
}