using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
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
        
//        public override Address CreateMultiSigAccount(AssociationAuth input)
//        {
//            var authorization = input;
//            Assert(authorization.Reviewers.Count > 0 && authorization.Reviewers.All(r => r.PubKey.Length > 0),
//                "Invalid authorization for multi signature.");
//            Address multiSigAccount = authorization.MultiSigAccount ??
//                                      Address.FromPublicKey(authorization.ToByteArray().ToArray());
//            var existing = State.MultiSig[multiSigAccount];
//            Assert(existing == null, "MultiSigAccount already existed.");
//            uint accumulatedWeights =
//                authorization.Reviewers.Aggregate<Reviewer, uint>(0, (weights, r) => weights + r.Weight);
//
//            // Weight accumulation should be more than authorization execution threshold.
//            Assert(accumulatedWeights >= authorization.ExecutionThreshold, "Invalid authorization.");
//
//            // At least one reviewer can propose.
//            bool canBeProposed = authorization.Reviewers.Any(r => r.Weight >= authorization.ProposerThreshold);
//            Assert(canBeProposed, "Invalid authorization.");
//
//            authorization.MultiSigAccount = multiSigAccount;
//            State.MultiSig[multiSigAccount] = authorization;
//            return multiSigAccount;
//        }

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
            var association = GetAssociation(null);
            Assert(association.Reviewers.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
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

            // check and append signatures to packed txn
            // check authorization of proposal
            //var proposedTxn = CheckAndFillTxnData(proposal, approved);
            // send deferred transaction
            //Context.SendDeferredTransaction(proposedTxn);
            
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
        #endregion

//        public override BoolValue IsMultiSigAccount(Address input)
//        {
//            var address = input;
//            var authorization = State.MultiSig[address];
//            if (address.Equals(Context.Genesis) || authorization != null)
//            {
//                return new BoolValue() {Value = true};
//            }
//
//            return new BoolValue() {Value = false};
//        }

//        private Transaction CheckAndFillTxnData(Proposal proposal, Approved approved)
//        {
//            // fill txn data 
//            var proposedTxn = GetPendingTxn(proposal.TxnData.ToByteArray());
//            foreach (var approval in approved.Approvals)
//            {
//                proposedTxn.Sigs.Add(approval.Signature);
//            }
//
//            proposedTxn.RefBlockNumber = Context.CurrentHeight;
//            proposedTxn.RefBlockPrefix = ByteString.CopyFrom(Context.PreviousBlockHash.Value.ToByteArray());
//            return proposedTxn;
//        }

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
                var recoverPublicKey = Context.RecoverPublicKey(approval.Signature.ToByteArray(), toSig);
                if (recoverPublicKey == null)
                    return weights;
                var reviewer = association.Reviewers.FirstOrDefault(r => r.PubKey.SequenceEqual(recoverPublicKey));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovalCount >= association.ExecutionThreshold;
        }
 
        private void CheckSignature(byte[] data, byte[] approvalSignature)
        {
            var recoveredPublicKey = Context.RecoverPublicKey(approvalSignature, data);
            var senderPublicKey = Context.RecoverPublicKey();
            Assert(recoveredPublicKey.SequenceEqual(senderPublicKey), "Incorrect signature");
        }


//        private void CheckTxnData(Address msigAddress, byte[] txnData)
//        {
//            var proposedTxn = GetPendingTxn(txnData);
//            Assert(proposedTxn.From.Equals(msigAddress),
//                "From address in proposed transaction is not valid multisig account.");
//            Assert(proposedTxn.Sigs.Count == 0, "Invalid signatures in proposed transaction.");
//        }
//
//        private uint SystemThreshold(uint reviewerCount)
//        {
//            return reviewerCount * 2 / 3;
//        }
    }
}