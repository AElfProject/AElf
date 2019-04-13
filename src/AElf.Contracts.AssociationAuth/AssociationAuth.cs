using System;
using System.Linq;
using Acs3;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthContract : AssociationAuthContractContainer.AssociationAuthContractBase
    {
        #region View

        public override Association GetAssociation(Empty input)
        {
            var association = State.Association.Value;
            Assert(association != null, "No registered association.");
            return association;
        }

        #endregion view

        #region Actions

        public override Empty Initialize(AssociationAuthContractInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ParliamentAuthContractSystemName.Value = input.ParliamentAuthContractSystemName;
            State.Owner.Value = input.Director;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty UpdateAssociation(Association input)
        {
            // check owner
            Assert(Context.Sender.Equals(State.Owner.Value), "Not authorized to update association.");
            State.Association.Value = input;
            return new Empty();
        }

        public override Hash CreateProposal(Proposal proposal)
        {
            // check validity of proposal
            Assert(
                proposal.Name != null
                && proposal.ToAddress != null
                && proposal.Params != null
                && proposal.Proposer != null, "Invalid proposal.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();

            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");

            Hash hash = Hash.FromMessage(proposal);
            var existing = State.Proposals[hash];
            Assert(existing == null, "Proposal already created.");

            // check authorization of proposer public key
            CheckProposerAuthority(proposal.Proposer);
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
            byte[] pubKey = Context.RecoverPublicKey();
            Assert(approval.PublicKey.IsEmpty || approval.PublicKey.ToByteArray().SequenceEqual(pubKey),
                "Invalid public key in approval.");
            var association = GetAssociation(null);
            Assert(association.Reviewers.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
                "Not authorized approval.");
            if(approval.PublicKey.IsEmpty)
                approval.PublicKey = ByteString.CopyFrom(pubKey);
            approved = approved ?? new ApprovedResult();
            approved.Approvals.Add(approval);
            State.Approved[hash] = approved;

            return new BoolValue {Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
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
                result.CanBeReleased = CheckApprovals(proposalId);
            }

            return result;
        }
        #endregion

        private void CheckProposerAuthority(Address proposer)
        {
            // Proposal should not be from multi sig account.
            // As a result, only check first public key.
            var association = GetAssociation(null);
            Reviewer reviewer = association.Reviewers.FirstOrDefault(r =>
                r.PubKey.Equals(ByteString.CopyFrom(Context.RecoverPublicKey())));
            var proposerPerm = reviewer?.Weight ?? 0;
            Assert(Context.Sender.Equals(proposer) &&
                   proposerPerm >= association.ProposerThreshold, "Unable to propose.");

            // No need to check authority if threshold is 0.
            // check packed transaction 
            //CheckTxnData(authorization.MultiSigAccount, proposal.TxnData.ToByteArray());
//            Assert(proposedTxn.From.Equals(msigAddress),
//                "From address in proposed transaction is not valid multisig account.");
        }

        private bool CheckApprovals(Hash proposalId)
        {
            var approved = State.Approved[proposalId];

            var toSig = proposalId.DumpByteArray();
            var association = GetAssociation(null);
            // processing approvals 
            var validApprovalCount = approved.Approvals.Aggregate((int) 0, (weights, approval) =>
            {
                var reviewer =
                    association.Reviewers.FirstOrDefault(r => r.PubKey.SequenceEqual(approval.PublicKey.ToByteArray()));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovalCount >= association.ExecutionThreshold;
        }
    }
}