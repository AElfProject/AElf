using System;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Authorization
{
    public partial class AuthorizationContract : AuthorizationContractContainer.AuthorizationContractBase
    {
        #region View

        public override Proposal GetProposal(Hash input)
        {
            var proposalHash = input;
            var proposal = State.Proposals[proposalHash];
            Assert(proposal != null, "Not found proposal.");

            if (proposal.Status == ProposalStatus.Released)
            {
                return proposal;
            }

            if (Context.CurrentBlockTime > proposal.ExpiredTime.ToDateTime())
            {
                proposal.Status = ProposalStatus.Expired;
            }
            else
            {
                var msigAccount = proposal.MultiSigAccount;
                var auth = GetAuthorization(msigAccount);
                Assert(auth != null, "Not found authorization."); // this should not happen.

                // check approvals
                var approved = State.Approved[proposalHash];

                proposal.Status = CheckApproval(approved, auth, proposal)
                    ? ProposalStatus.Decided
                    : ProposalStatus.ToBeDecided;
            }

            return proposal;
        }

        public override Kernel.Authorization GetAuthorization(Address input)
        {
            var address = input;
            // case 1: get authorization of normal multi sig account
            if (!address.Equals(Context.Genesis))
            {
                var authorization = State.MultiSig[address];
                Assert(authorization != null, "MultiSigAccount not found.");
                return authorization;
            }

            // case 2: get authorization of system account  
            var reviewers = GetMiners().PublicKeys;
            var auth = new Kernel.Authorization
            {
                MultiSigAccount = Context.Genesis,
                ExecutionThreshold = SystemThreshold((uint) reviewers.Count),
                ProposerThreshold = 0
            };
            auth.Reviewers.AddRange(reviewers.Select(r => new Reviewer
            {
                PubKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(r)),
                Weight = 1 // BP weight
            }));

            return auth;
        }

        #endregion view

        #region Actions

        public override Address CreateMultiSigAccount(Kernel.Authorization input)
        {
            var authorization = input;
            Assert(authorization.Reviewers.Count > 0 && authorization.Reviewers.All(r => r.PubKey.Length > 0),
                "Invalid authorization for multi signature.");
            Address multiSigAccount = authorization.MultiSigAccount ??
                                      Address.FromPublicKey(authorization.ToByteArray().ToArray());
            var existing = State.MultiSig[multiSigAccount];
            Assert(existing == null, "MultiSigAccount already existed.");
            uint accumulatedWeights =
                authorization.Reviewers.Aggregate<Reviewer, uint>(0, (weights, r) => weights + r.Weight);

            // Weight accumulation should be more than authorization execution threshold.
            Assert(accumulatedWeights >= authorization.ExecutionThreshold, "Invalid authorization.");

            // At least one reviewer can propose.
            bool canBeProposed = authorization.Reviewers.Any(r => r.Weight >= authorization.ProposerThreshold);
            Assert(canBeProposed, "Invalid authorization.");

            authorization.MultiSigAccount = multiSigAccount;
            State.MultiSig[multiSigAccount] = authorization;
            return multiSigAccount;
        }

        public override Hash Propose(Proposal input)
        {
            var proposal = input;
            // check validity of proposal
            Assert(
                proposal.Name != null
                && proposal.MultiSigAccount != null
                && proposal.TxnData != null
                && proposal.Status == ProposalStatus.ToBeDecided
                && proposal.Proposer != null, "Invalid proposal.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();

            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");

            Hash hash = proposal.GetHash();
            var existing = State.Proposals[hash];
            Assert(existing == null, "Proposal already created.");

            // check authorization of proposer public key
            var auth = GetAuthorization(proposal.MultiSigAccount);
            CheckAuthority(proposal, auth);
            State.Proposals[hash] = proposal;
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

            var proposal = State.Proposals[hash];
            // check authorization and permission 
            Assert(proposal != null, "Proposal not found.");
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),
                "Expired proposal.");

            var msig = proposal.MultiSigAccount;
            var authorization = GetAuthorization(msig);
            byte[] toSig = proposal.TxnData.ToByteArray().CalculateHash();
            byte[] pubKey = Context.RecoverPublicKey(approval.Signature.ToByteArray(), toSig);
            Assert(Context.RecoverPublicKey().SequenceEqual(pubKey), "Invalid approval.");
            Assert(authorization.Reviewers.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)),
                "Not authorized approval.");

            CheckSignature(proposal.TxnData.ToByteArray(), approval.Signature.ToByteArray());
            approved = approved ?? new Approved();
            approved.Approvals.Add(approval);
            State.Approved[hash] = approved;

            if (CheckApproval(approved, authorization, proposal))
            {
                // Executing threshold already reached.
                State.Proposals[hash] = proposal;
            }

            return new BoolValue() {Value = true};
        }

        public override Hash Release(Hash input)
        {
            var proposalHash = input;
            var proposal = State.Proposals[proposalHash];
            Assert(proposal != null, "Proposal not found.");
            // check expired time of proposal
            Assert(Context.CurrentBlockTime < proposal.ExpiredTime.ToDateTime(),
                "Expired proposal.");
            Assert(proposal.Status != ProposalStatus.Released, "Proposal already released");

            var msigAccount = proposal.MultiSigAccount;
            var auth = GetAuthorization(msigAccount);

            // check approvals
            var approved = State.Approved[proposalHash];
            Assert(CheckApproval(approved, auth, proposal), "Not authorized to release.");

            // check and append signatures to packed txn
            // check authorization of proposal
            var proposedTxn = CheckAndFillTxnData(proposal, approved);
            // send deferred transaction
            Context.SendDeferredTransaction(proposedTxn);
            proposal.Status = ProposalStatus.Released;
            State.Proposals[proposalHash] = proposal;
            return proposedTxn.GetHash();
        }

        #endregion

        public override BoolValue IsMultiSigAccount(Address input)
        {
            var address = input;
            var authorization = State.MultiSig[address];
            if (address.Equals(Context.Genesis) || authorization != null)
            {
                return new BoolValue() {Value = true};
            }

            return new BoolValue() {Value = false};
        }

        /*private bool GetAuth(Address address, out Authorization authorization)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return false;
        }*/

        private Transaction CheckAndFillTxnData(Proposal proposal, Approved approved)
        {
            // fill txn data 
            var proposedTxn = GetPendingTxn(proposal.TxnData.ToByteArray());
            foreach (var approval in approved.Approvals)
            {
                proposedTxn.Sigs.Add(approval.Signature);
            }

            proposedTxn.RefBlockNumber = Context.CurrentHeight;
            proposedTxn.RefBlockPrefix = ByteString.CopyFrom(Context.PreviousBlockHash.Value.ToByteArray());
            return proposedTxn;
        }

        private void CheckAuthority(Proposal proposal, Kernel.Authorization authorization)
        {
            if (authorization.ProposerThreshold > 0)
            {
                // Proposal should not be from multi sig account.
                // As a result, only check first public key.
                Reviewer reviewer = authorization.Reviewers.FirstOrDefault(r =>
                    r.PubKey.Equals(ByteString.CopyFrom(Context.RecoverPublicKey())));
                var proposerPerm = reviewer?.Weight ?? 0;
                Assert(Context.Sender.Equals(proposal.Proposer) &&
                       proposerPerm >= authorization.ProposerThreshold, "Unable to propose.");
            }

            // No need to check authority if threshold is 0.
            // check packed transaction 
            CheckTxnData(authorization.MultiSigAccount, proposal.TxnData.ToByteArray());
        }

        private bool CheckApproval(Approved approved, Kernel.Authorization authorization, Proposal proposal)
        {
            byte[] toSig = proposal.TxnData.ToByteArray().CalculateHash();

            // processing approvals 
            var validApprovalCount = approved.Approvals.Aggregate((ulong) 0, (weights, approval) =>
            {
                var canBeRecovered =
                    CryptoHelpers.RecoverPublicKey(approval.Signature.ToByteArray(), toSig, out var recovered);
                if (!canBeRecovered)
                    return weights;
                var reviewer = authorization.Reviewers.FirstOrDefault(r => r.PubKey.SequenceEqual(recovered));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovalCount >= authorization.ExecutionThreshold;
        }

        private void CheckSignature(byte[] txnData, byte[] approvalSignature)
        {
            var proposedTxn = GetPendingTxn(txnData);
            proposedTxn.Sigs.Add(ByteString.CopyFrom(approvalSignature));
            Assert(Context.VerifySignature(proposedTxn), "Incorrect signature");
        }

        private void CheckTxnData(Address msigAddress, byte[] txnData)
        {
            var proposedTxn = GetPendingTxn(txnData);
            Assert(proposedTxn.From.Equals(msigAddress),
                "From address in proposed transaction is not valid multisig account.");
            Assert(proposedTxn.Sigs.Count == 0, "Invalid signatures in proposed transaction.");
        }

        private uint SystemThreshold(uint reviewerCount)
        {
            return reviewerCount * 2 / 3;
        }

        private Transaction GetPendingTxn(byte[] txnData)
        {
            return Transaction.Parser.ParseFrom(txnData);
        }
    }
}