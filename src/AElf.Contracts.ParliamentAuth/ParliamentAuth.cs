using System;
using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract : ParliamentAuthContractContainer.ParliamentAuthContractBase
    {
        public override Empty Initialize(ParliamentAuthInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ConsensusContractSystemName.Value = input.ConsensusContractSystemName;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Hash CreateProposal(Proposal input)
        {
            var proposal = input;
            // check validity of proposal
            Assert(
                proposal.Name != null
                //&& proposal.MultiSigAccount != null
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
                Proposal = proposal,
                IsReleased = false
            };
            return hash;
        }

        public override BoolValue Approve(Approval input)
        {
            var approval = input;
            // check validity of proposal 
            Hash hash = approval.ProposalHash;

            var approved = State.Approved[hash];
            // check approval not existed
            Assert(approved == null || !approved.Approvals.Contains(approval),
                "Approval already existed.");

            var proposalInfo = State.Proposals[hash];
            // check authorization and permission 
            Assert(proposalInfo != null, "Proposal not found.");
            var proposal = proposalInfo.Proposal;
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(), 
                "Expired proposal.");
            //byte[] toSig = Hash.FromMessage(proposal).DumpByteArray();
            byte[] pubKey = Context.RecoverPublicKey();
            Assert(approval.PublicKey.IsEmpty || approval.PublicKey.ToByteArray().SequenceEqual(pubKey),
                "Invalid public key in approval.");

            var representatives = GetRepresentatives();
            Assert(representatives.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
                "Not authorized approval.");

            if(approval.PublicKey.IsEmpty)
                approval.PublicKey = ByteString.CopyFrom(pubKey);
            approved = approved ?? new ApprovedResult();
            approved.Approvals.Add(approval);
            State.Approved[hash] = approved;

            return new BoolValue {Value = true};
        }

        public override Empty Release(Hash input)
        {
            var proposalId = input;
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            var proposal = proposalInfo.Proposal;
            // check expired time of proposal
            
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),
                "Expired proposal.");
            Assert(!proposalInfo.IsReleased, "Proposal already released");

            // check approvals
            Assert(CheckApprovals(proposalId), "Not authorized to release.");
           
            Context.SendInline(proposal.ToAddress, proposal.Name, proposal.Params);
            proposalInfo.IsReleased = true;
            State.Proposals[proposalId] = proposalInfo;
            return new Empty();
        }

        public override GetProposalOutput GetProposal(Hash input)
        {
            var proposalId = input;
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
                result.CanBeReleased = CheckApprovals(proposalId);
            }

            return result;
        }
    }
}