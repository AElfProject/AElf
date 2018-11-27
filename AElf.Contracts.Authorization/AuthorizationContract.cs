using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types.Proposal;
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
        public static readonly string MultiSig = GlobalConfig.AElfMultiSig;
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
    
    public class AuthorizationContract : CSharpSmartContract
    {
        
        private readonly Map<Address, Kernel.Types.Proposal.Authorization> _multiSig = new Map<Address, Kernel.Types.Proposal.Authorization>(FieldNames.MultiSig);
        private readonly Map<Hash, Proposal> _proposals = new Map<Hash, Proposal>(FieldNames.Proposal);
        private readonly Map<Hash, Approved> _approved = new Map<Hash, Approved>(FieldNames.Approved);
        //private readonly ProposalSerialNumber _proposalSerialNumber = ProposalSerialNumber.Instance;
        #region Actions

        public Address CreateMultiSigAccount(Kernel.Types.Proposal.Authorization authorization)
        {
            // TODO: check public key
            
            Address address = authorization.MultiSigAccount ?? Address.FromRawBytes(authorization.ToByteArray());
            Api.Assert(_multiSig.GetValue(address) == null, "MultiSigAccount already existed.");
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
                && proposal.ProposerPublicKey != null, "Invalid proposal.");
            Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            
            Hash hash = proposal.GetHash();
            Api.Assert(_proposals.GetValue(hash) == null, "Proposal already created.");
            
            // check authorization of proposer public key
            var auth = _multiSig.GetValue(proposal.MultiSigAccount);
            Api.Assert(auth != null, "MultiSigAccount not found.");
            CheckAuthority(proposal, auth);
            
            _proposals.SetValue(hash, proposal);
            return hash;
        }

        public bool SayYes(Approval approval)
        {
            // check validity of proposal 
            Hash hash = approval.ProposalHash;
            
            // check approval not existed
            var approved = _approved.GetValue(hash);
            Api.Assert(approved == null || !approved.Approvals.Contains(approval), "Approval already existed.");

            // check authorization and permission 
            CheckAuthority(approval);
            approved = approved ?? new Approved
            {
                ProposalHash = hash
            };
            approved.Approvals.Add(approval);
            _approved.SetValue(hash, approved);
            return true;
        }

        public Hash Release(Hash proposalHash)
        {
            var proposal = _proposals.GetValue(proposalHash);
            Api.Assert(proposal != null, "Proposal not found");
            // check expired time of proposal
            Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            
            // check and append signatures to packed txn
            var proposedTxn = CheckAndFillTxnData(proposal);
            // send deferred transaction
            Api.SendDeferredTransaction(proposedTxn);
            return proposedTxn.GetHash();
        }
        
        #endregion 
        
        public Kernel.Types.Proposal.Authorization GetProposal(Hash address)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return null;
        }
        
        public Kernel.Types.Proposal.Authorization GetAuth(Address address)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return null;
        }

        /*private bool GetAuth(Address address, out Authorization authorization)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
            return false;
        }*/

        private Transaction CheckAndFillTxnData(Proposal proposal)
        {
            // check authorization of proposal
            var msigAccount = proposal.MultiSigAccount;
            var auth = _multiSig.GetValue(msigAccount);
            Api.Assert(auth != null); // this should not happen.
            var proposalHash = proposal.GetHash();
            var approved = _approved.GetValue(proposalHash);
            CheckAuthority(approved, auth);
            
            // fill txn data 
            var proposedTxn = proposal.TxnData.GetTransaction();
            foreach (var approval in approved.Approvals)
            {
                proposedTxn.Sigs.Add(approval.Signature);
            }

            proposedTxn.RefBlockNumber = Api.GetCurrentHeight();
            proposedTxn.RefBlockPrefix = ByteString.CopyFrom(Api.GetPreviousBlockHash().Value.ToByteArray());
            return proposedTxn;
        }
        
        private void CheckAuthority(Proposal proposal, Kernel.Types.Proposal.Authorization authorization)
        {
            var from = Api.GetTransaction().From;
            Api.Assert(Address.FromRawBytes(proposal.ProposerPublicKey.ToByteArray()).Equals(from),
                "Proposal not created by sender.");

            List<Reviewer> reviewers = proposal.MultiSigAccount.Equals(Address.Genesis)
                ? Api.GetSystemReviewers()
                : authorization.Reviewers.ToList();
            Reviewer reviewer = reviewers.First(r => r.PubKey.Equals(proposal.ProposerPublicKey));
            var proposerPerm = reviewer?.Weight ?? 0; 
            Api.Assert(proposerPerm >= authorization.ProposerThreshold, "Not authorized to propose.");
            
            // check packed transaction 
            CheckTxnData(authorization.MultiSigAccount, proposal.TxnData);
        }
        
        private void CheckAuthority(Approval approval)
        {
            Hash hash = approval.ProposalHash;
            var proposal = _proposals.GetValue(hash);
            Api.Assert(proposal != null, "Proposal not found.");

            var msig = proposal.MultiSigAccount;
            var authorization = _multiSig.GetValue(msig);
            Api.Assert(authorization != null, "Authorization not found."); // should never happen
            Api.Assert(authorization.Reviewers.Any(r => r.PubKey.Equals(approval.Signature.P)),
                "Not authorized approval.");
            VerifySignature(proposal.TxnData, approval.Signature);
        }

        private void CheckAuthority(Approved approved, Kernel.Types.Proposal.Authorization authorization)
        {
            uint weight = 0;
            var validApprovals = approved.Approvals.All(a =>
            {
                var reviewer = authorization.Reviewers.FirstOrDefault(r => r.PubKey.Equals(a.Signature.P));
                if (reviewer == null)
                    return false;
                weight += reviewer.Weight;
                return true;
            });
            Console.WriteLine("weight: {0}", weight);
            Console.WriteLine("authorization.ExecutionThreshold: {0}", authorization.ExecutionThreshold);
            Api.Assert(validApprovals, "Unauthorized approval."); //this should not happen.
            Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
        }

        private void VerifySignature(PendingTxn txnData, Sig approvalSignature)
        {
            var proposedTxn = txnData.GetTransaction();
            proposedTxn.Sigs.Add(approvalSignature);
            Api.Assert(Api.VerifySignature(proposedTxn), "Incorrect signature");
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