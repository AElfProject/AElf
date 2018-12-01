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
        public static readonly string Proposal = GlobalConfig.AElfProposal;
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

        public byte[] CreateMultiSigAccount(Kernel.Types.Proposal.Authorization authorization)
        {
            // TODO: check public key

            Address multiSigAccount = authorization.MultiSigAccount ??
                                      Address.FromRawBytes(authorization.ToByteArray().Take(GlobalConfig.AddressLength)
                                          .ToArray());
            Api.Assert(_multiSig.GetValue(multiSigAccount).Equals(new Kernel.Types.Proposal.Authorization()),
                "MultiSigAccount already existed.");
            authorization.MultiSigAccount = multiSigAccount;
            _multiSig.SetValue(multiSigAccount, authorization);
            
            return multiSigAccount.DumpByteArray();
        }
        
        public byte[] Propose(Proposal proposal)
        {
            // check validity of proposal
            Api.Assert(
                proposal.Name != null
                && proposal.MultiSigAccount != null
                && proposal.TxnData != null
                && proposal.Status == ProposalStatus.ToBeDecided
                && proposal.Proposer != null, "Invalid proposal.");
            Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            
            Hash hash = proposal.GetHash();
            Api.Assert(_proposals.GetValue(hash).Equals(new Proposal()) , "Proposal already created.");
            
            // check authorization of proposer public key
            var auth = _multiSig.GetValue(proposal.MultiSigAccount);
            Api.Assert(!auth.Equals(new Kernel.Types.Proposal.Authorization()), "MultiSigAccount not found.");
            CheckAuthority(proposal, auth);
            
            _proposals.SetValue(hash, proposal);
            return hash.DumpByteArray();
        }

        public bool SayYes(Approval approval)
        {
            // check validity of proposal 
            Hash hash = approval.ProposalHash;
            
            // check approval not existed
            var approved = _approved.GetValue(hash);
            Api.Assert(approved.Equals(new Approved()) || !approved.Approvals.Contains(approval), "Approval already existed.");

            // check authorization and permission 
            var proposal = _proposals.GetValue(hash);
            Api.Assert(!proposal.Equals(new Proposal()), "Proposal not found.");
            
            var msig = proposal.MultiSigAccount;
            var authorization = _multiSig.GetValue(msig);
            
            Api.Assert(!authorization.Equals(new Kernel.Types.Proposal.Authorization()), "Authorization not found."); // should never happen
            Api.Assert(authorization.Reviewers.Any(r => r.PubKey.Equals(approval.Signature.P)),
                "Not authorized approval.");
            
            VerifySignature(proposal.TxnData, approval.Signature);

            approved.Approvals.Add(approval);
            _approved.SetValue(hash, approved);
            if (CheckPermission(approved, authorization))
            {
                // Executing threshold already reached.
                proposal.Status = ProposalStatus.Decided;
                _proposals.SetValue(hash, proposal);
            }
            
            return true;
        }

        public byte[] Release(Hash proposalHash)
        {
            var proposal = _proposals.GetValue(proposalHash);
            Api.Assert(!proposal.Equals(new Proposal()), "Proposal not found");
            // check expired time of proposal
            Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            Api.Assert(proposal.Status != ProposalStatus.Released, "Proposal already released");
            
            var msigAccount = proposal.MultiSigAccount;
            var auth = _multiSig.GetValue(msigAccount);
            Api.Assert(!auth.Equals(new Kernel.Types.Proposal.Authorization())); // this should not happen.
            
            // check approvals
            var approved = _approved.GetValue(proposalHash);
            Api.Assert(proposal.Status == ProposalStatus.Decided && CheckPermission(approved, auth),
                "Not authorized to release.");
            
            // check and append signatures to packed txn
            // check authorization of proposal
            var proposedTxn = CheckAndFillTxnData(proposal, approved);
            // send deferred transaction
            Api.SendDeferredTransaction(proposedTxn);
            proposal.Status = ProposalStatus.Released;
            _proposals.SetValue(proposalHash, proposal);
            return proposedTxn.GetHash().DumpByteArray();
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
            // case 1: get authorization of normal multi sig account
            if (!address.Equals(Address.Genesis)) 
                return _multiSig.GetValue(address);
            // case 2: get authorization of system account  
            var reviewers = Api.GetSystemReviewers();
            var auth =  new Kernel.Types.Proposal.Authorization
            {
                MultiSigAccount = Address.Genesis,
                ExecutionThreshold = (uint) (reviewers.Count * 2 / 3),
                ProposerThreshold = 1
            };
            auth.Reviewers.AddRange(reviewers);
            
            return auth;
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
            List<Reviewer> reviewers = proposal.MultiSigAccount.Equals(Address.Genesis)
                ? Api.GetSystemReviewers()
                : authorization.Reviewers.ToList();
            
            // Proposal should not be from multi sig account.
            // As a result, only check first public key.
            Reviewer reviewer = reviewers.FirstOrDefault(r => r.PubKey.Equals(Api.GetPublicKey()));
            var proposerPerm = reviewer?.Weight ?? 0;
            Api.Assert(
                Api.GetTransactionFromAddress().Equals(proposal.Proposer) &&
                proposerPerm >= authorization.ProposerThreshold, "Not authorized to propose.");
            
            // check packed transaction 
            CheckTxnData(authorization.MultiSigAccount, proposal.TxnData);
        }
        
        private bool CheckPermission(Approved approved, Kernel.Types.Proposal.Authorization authorization)
        {
            uint weight = 0;
            var validApprovals = approved.Approvals.All(a =>
            {
                var reviewer = authorization.Reviewers.FirstOrDefault(r => r.PubKey.Equals(a.Signature.P));
                if (reviewer == null )
                    return false;
                weight += reviewer.Weight;
                return true;
            });
            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovals && weight >= authorization.ExecutionThreshold;
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
            Api.Assert(proposedTxn.From.Equals(msigAccount),
                "From address in proposed transaction is not valid multisig account.");
            Api.Assert(proposedTxn.Sigs.Count == 0, "Invalid signatures in proposed transaction.");
            Api.Assert(proposedTxn.Type == TransactionType.MsigTransaction, "Incorrect proposed transaction type.");
        }
    }
}