using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types.Auth;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Authorization
{
    #region Field Names

    public static class FieldNames
    {
        public static readonly string MultiSig = "__MultiSig__";
        public static readonly string Proposal = "__Proposal__";
        public static readonly string Pending = "__Pending__";
        public static readonly string Approved = "__Approved__";
        public static readonly string ProposalSerialNumber = "__ProposalSerialNumber__";
    }

    #endregion Field Names
    
    #region Customized Field Types

    /*internal class ProposalSerialNumber : UInt64Field
    {
        internal static ProposalSerialNumber Instance { get; } = new ProposalSerialNumber();

        private ProposalSerialNumber() : this(FieldNames.ProposalSerialNumber)
        {
        }

        private ProposalSerialNumber(string name) : base(name)
        {
        }

        private ulong _value;

        public ulong Value
        {
            get
            {
                if (_value == 0)
                {
                    _value = GetValue();
                }

                if (GlobalConfig.BasicContractZeroSerialNumber > _value)
                {
                    _value = GlobalConfig.BasicContractZeroSerialNumber;
                }
                return _value;
            }
            private set => _value = value;
        }

        public ProposalSerialNumber Increment()
        {
            Value = Value + 1;
            SetValue(Value);
            GlobalConfig.BasicContractZeroSerialNumber = Value;
            return this;
        }
    }*/

    #endregion Customized Field Types
    
    public class Authorization : CSharpSmartContract
    {
        
        private readonly Map<Address, Auth> _multiSig = new Map<Address, Auth>(FieldNames.MultiSig);
        private readonly Map<Hash, Proposal> _proposals = new Map<Hash, Proposal>(FieldNames.Proposal);
        private readonly Map<Hash, Approved> _approved = new Map<Hash, Approved>(FieldNames.Approved);
        //private readonly ProposalSerialNumber _proposalSerialNumber = ProposalSerialNumber.Instance;
        #region Actions

        public Address CreateMultiSigAccount(Auth authorization)
        {
            // TODO: check public key
            
            Address address = Address.FromRawBytes(authorization.ToByteArray());
            Api.Assert(_multiSig.GetValue(address) != null, "MultiSigAccount already created.");
            // check permissions
            Api.Assert(authorization.Reviewers.Count >= authorization.ExecutionThreshold,
                "Threshold should not be bigger than reviewer count.");
            _multiSig.SetValue(address, authorization);
            
            return address;
        }
        
        public Hash Propose(Proposal proposal)
        {
            // check validity of proposal
            Api.Assert(
                proposal.Name != null
                && proposal.MultiSigAccount != null
                && proposal.TxnData != null
                && proposal.Proposer != null, "Invalid proposal.");
            Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            
            Hash hash = Hash.FromRawBytes(proposal.ToByteArray());
            Api.Assert(_proposals.GetValue(hash) != null, "Proposal already created.");
            
            // check authorization of proposer public key
            var auth = _multiSig.GetValue(proposal.MultiSigAccount);
            Api.Assert(auth != null, "MultiSigAccount not found.");
            CheckAuthorization(proposal, auth);
            
            _proposals.SetValue(hash, proposal);
            return hash;
        }

        public bool SayYes(Approval approval)
        {
            // check validity of proposal 
            Hash hash = Hash.FromMessage(approval.ProposalHash);
            var proposal = _proposals.GetValue(hash);
            Api.Assert(proposal != null && proposal.Equals(approval.Proposal), "Proposal not found");

            // check approval not existed
            var approved = _approved.GetValue(hash);
            Api.Assert(!approved.Approvals.Contains(approval), "Approval already existed.");
            
            // check authorization and permission 
            CheckAuthorization(approval);
            
            return true;
        }

        public Hash Release(Hash proposalHash)
        {
            var proposal = _proposals.GetValue(proposalHash);
            Api.Assert(proposal != null, "Proposal not found");
            // check expired time of proposal
            Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            
            // check authorization of proposal
            var msigAccount = proposal.MultiSigAccount;
            var auth = _multiSig.GetValue(msigAccount);
            Api.Assert(auth != null); // this should not happen.
            var approved = _approved.GetValue(proposalHash);       
            CheckAuthorization(approved, auth);

            // check and append signatures to packed txn
            var proposedTxn = proposal.TxnData.GetTransaction();
            foreach (var approval in approved.Approvals)
            {
                proposedTxn.Sigs.Add(approval.Signature);
            }

            proposedTxn.RefBlockNumber = Api.GetCurrentHeight();
            proposedTxn.RefBlockPrefix = ByteString.CopyFrom(Api.GetPreviousBlockHash().Value.ToByteArray());

            // send deferred transaction
            Api.SendDeferredTransaction();
            return proposedTxn.GetHash();
        }
        
        #endregion
        
        public Auth GetProposal(Hash address)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return null;
        }
        
        public Auth GetAuth(Address address)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return null;
        }


        /*private bool GetAuth(Address address, out Auth authorization)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return false;
        }*/

        private void CheckAuthorization(Proposal proposal,Auth auth)
        {
            var from = Api.GetTransaction().From;
            Api.Assert(proposal.Proposer.Equals(from), "Proposal not created by sender.");

            List<Reviewer> reviewers = proposal.MultiSigAccount.Equals(Address.Genesis)
                ? Api.GetSystemReviewers()
                : auth.Reviewers.ToList();
            Reviewer reviewer = reviewers.First(r => Address.FromRawBytes(r.PubKey.ToByteArray()).Equals(proposal.Proposer));
            var proposerPerm = reviewer?.Weight ?? 0; 
            Api.Assert(proposerPerm >= auth.ProposerThreshold, "Not authorized to propose.");
            
            // check packed transaction 
            CheckTxnData(auth.MultiSigAccount, proposal.TxnData);
        }
        
        
        private void CheckAuthorization(Approval approval)
        {
            Hash hash = approval.ProposalHash;
            var proposal = _proposals.GetValue(hash);
            Api.Assert(proposal != null && proposal.Equals(approval.Proposal), "Invalid Approval.");
            VerifySignature(proposal.TxnData, approval.Signature);
        }

        private void CheckAuthorization(Approved approved, Auth auth)
        {
            uint threshold = auth.ExecutionThreshold;
            uint weight = 0;
            var validApprovals = approved.Approvals.All(a =>
            {
                var reviewer = auth.Reviewers.FirstOrDefault(r => r.PubKey.Equals(a.Signature.P));
                if (reviewer == null)
                    return false;
                weight += reviewer.Weight;
                return true;
            });
            Api.Assert(validApprovals, "Unauthorized approval."); //this should not happen.
            Api.Assert(weight >= threshold, "Not enough approvals.");
        }

        private void VerifySignature(PendingTxn txnData, Sig approvalSignature)
        {
            var proposedTxn = txnData.GetTransaction();
            proposedTxn.Sigs.Add(approvalSignature);
            Api.VerifySignature(proposedTxn);
        }

        private void CheckTxnData(Address msigAccount, PendingTxn txnData)
        {
            var proposedTxn = txnData.GetTransaction();
            // packed transaction should not be signed.
            Api.Assert(
                proposedTxn.From.Equals(msigAccount)
                && proposedTxn.Sigs.Count == 0 
                && proposedTxn.Type == TransactionType.MsigTransaction,
                "Invalid proposed transaction."
                );
        }

    }
}