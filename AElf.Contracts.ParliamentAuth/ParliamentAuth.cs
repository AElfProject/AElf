using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthContract : ParliamentAuthContractContainer.ParliamentAuthContractBase
    {
        public override Empty Initialize(ParliamentAuthInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ConsensusContractSystemName.Value = input.ConsensusContractSystemName;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Hash Propose(Proposal input)
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

            Hash hash = proposal.GetHash();
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

        public override BoolValue SayYes(Approval input)
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
            byte[] toSig = proposal.GetHash().DumpByteArray();
            byte[] pubKey = Context.RecoverPublicKey(approval.Signature.ToByteArray(), toSig);
            Assert(pubKey != null && Context.RecoverPublicKey().SequenceEqual(pubKey), "Invalid approval.");
            var representatives = GetRepresentatives();
            Assert(representatives.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
                "Not authorized approval.");

            CheckSignature(proposal.GetHash().DumpByteArray(), approval.Signature.ToByteArray());
            approved = approved ?? new Approved();
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
           
            // temporary method to calculate virtual hash 
            var virtualHash = Hash.FromMessage(proposal.ToAddress);
            Context.SendVirtualInline(virtualHash, proposal.ToAddress, proposal.Name, proposal.Params);
            
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
        
        private int SystemThreshold(int reviewerCount)
        {
            return reviewerCount * 2 / 3;
        }

        private IEnumerable<Representative> GetRepresentatives()
        {
            var minerList = State.ConsensusContractReferenceState.GetCurrentMiners.Call(new Empty());
            var representatives = minerList.PublicKeys.Select(publicKey => new Representative
            {
                PubKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey)),
                Weight = 1 // weight is for farther improvement
            });
            return representatives;
        }
        
        private void CheckSignature(byte[] data, byte[] approvalSignature)
        {
            var recoveredPublicKey = Context.RecoverPublicKey(approvalSignature, data);
            var senderPublicKey = Context.RecoverPublicKey();
            Assert(recoveredPublicKey.SequenceEqual(senderPublicKey), "Incorrect signature");
        }
        
        private bool CheckApprovals(Hash proposalId)
        {
            var approved = State.Approved[proposalId];

            var toSig = proposalId.DumpByteArray();
            var representatives = GetRepresentatives();

            // processing approvals 
            var validApprovalCount = approved.Approvals.Aggregate((int) 0, (weights, approval) =>
            {
                var recoverPublicKey = Context.RecoverPublicKey(approval.Signature.ToByteArray(), toSig);
                if (recoverPublicKey == null)
                    return weights;
                var reviewer = representatives.FirstOrDefault(r => r.PubKey.SequenceEqual(recoverPublicKey));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovalCount >= SystemThreshold(representatives.Count());
        }
    }
}