using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
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
        private readonly Map<Address, Kernel.Authorization> _multiSig =
            new Map<Address, Kernel.Authorization>(FieldNames.MultiSig);

        private readonly Map<Hash, Proposal> _proposals = new Map<Hash, Proposal>(FieldNames.Proposal);
        private readonly Map<Hash, Approved> _approved = new Map<Hash, Approved>(FieldNames.Approved);
        private Address Genesis { get;} = Address.Genesis;
        //private readonly ProposalSerialNumber _proposalSerialNumber = ProposalSerialNumber.Instance;

        #region Actions

        public byte[] CreateMultiSigAccount(Kernel.Authorization authorization)
        {
            Api.Assert(authorization.Reviewers.Count > 0 && authorization.Reviewers.All(r => r.PubKey.Length > 0),
                "Invalid authorization for multi signature.");
            // TODO: check public key -- if no Multisig account then ELF_chainID_SHA^2(authorization)
            Address multiSigAccount = authorization.MultiSigAccount ??
                                      Address.FromPublicKey(Api.ChainId.DumpByteArray(),
                                          authorization.ToByteArray().ToArray());
            Api.Assert(_multiSig.GetValue(multiSigAccount).Equals(new Kernel.Authorization()),
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
            DateTime timestamp = TimerHelper.ConvertFromUnixTimestamp(proposal.ExpiredTime);

            Api.Assert(DateTime.UtcNow < timestamp, "Expired proposal.");

            Hash hash = proposal.GetHash();
            Api.Assert(_proposals.GetValue(hash).Equals(new Proposal()), "Proposal already created.");

            // check authorization of proposer public key
            var auth = GetAuth(proposal.MultiSigAccount);
            Api.Assert(!auth.Equals(new Kernel.Authorization()), "MultiSigAccount not found.");
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
            Api.Assert(approved.Equals(new Approved()) || !approved.Approvals.Contains(approval),
                "Approval already existed.");

            // check authorization and permission 
            var proposal = _proposals.GetValue(hash);
            Api.Assert(!proposal.Equals(new Proposal()), "Proposal not found.");
            //Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            
            var msig = proposal.MultiSigAccount;
            var authorization = GetAuth(msig);
            
            Api.Assert(!authorization.Equals(new Kernel.Authorization()), "Authorization not found."); // should never happen
            
            byte[] toSig = proposal.TxnData.ToByteArray().CalculateHash();
            byte[] pubKey = Api.RecoverPublicKey(approval.Signature.ToByteArray(), toSig);
            Api.Assert(Api.RecoverPublicKey().SequenceEqual(pubKey), "Invalid approval.");
            Api.Assert(authorization.Reviewers.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)), "Not authorized approval.");
            
            CheckSignature(proposal.TxnData.ToByteArray(), approval.Signature.ToByteArray());
            approved.Approvals.Add(approval);
            _approved.SetValue(hash, approved);

            if (CheckPermission(approved, authorization, proposal))
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
            //Api.Assert(DateTime.UtcNow < proposal.ExpiredTime.ToDateTime(), "Expired proposal.");
            Api.Assert(proposal.Status != ProposalStatus.Released, "Proposal already released");
            
            var msigAccount = proposal.MultiSigAccount;
            var auth = GetAuth(msigAccount);
            Api.Assert(!auth.Equals(new Kernel.Authorization())); // this should not happen.

            // check approvals
            var approved = _approved.GetValue(proposalHash);

            Api.Assert(proposal.Status == ProposalStatus.Decided && CheckPermission(approved, auth, proposal),
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

        public Kernel.Authorization GetProposal(Hash address)
        {
            // case 1
            // get authorization of system account

            // case 2 
            // get authorization of normal multi sig account
            return null;
        }

        public Kernel.Authorization GetAuth(Address address)
        {
            // case 1: get authorization of normal multi sig account
            if (!address.Equals(Genesis))
                return _multiSig.GetValue(address);
            // case 2: get authorization of system account  
            var reviewers = Api.GetSystemReviewers();
            var auth = new Kernel.Authorization
            {
                MultiSigAccount = Genesis,
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

            proposedTxn.RefBlockNumber = Api.GetCurrentHeight();
            proposedTxn.RefBlockPrefix = ByteString.CopyFrom(Api.GetPreviousBlockHash().Value.ToByteArray());
            return proposedTxn;
        }

        private void CheckAuthority(Proposal proposal, Kernel.Authorization authorization)
        {
            if (authorization.ProposerThreshold > 0)
            {
                // Proposal should not be from multi sig account.
                // As a result, only check first public key.
                Reviewer reviewer = authorization.Reviewers.FirstOrDefault(r => r.PubKey.Equals(ByteString.CopyFrom(Api.RecoverPublicKey())));
                var proposerPerm = reviewer?.Weight ?? 0;
                Api.Assert(
                    Api.GetFromAddress().Equals(proposal.Proposer) &&
                    proposerPerm >= authorization.ProposerThreshold, "Not authorized to propose.");
            }
            // No need to check authority if threshold is 0.
            // check packed transaction 
            CheckTxnData(authorization.MultiSigAccount, proposal.TxnData.ToByteArray());
        }

        private bool CheckPermission(Approved approved, Kernel.Authorization authorization,
            Proposal proposal)
        {
            uint weight = 0;
            byte[] toSig = proposal.TxnData.ToByteArray().CalculateHash();
            
            // processing approvals 
            var validApprovals = approved.Approvals.All(a =>
            {
                var recovered = CryptoHelpers.RecoverPublicKey(a.Signature.ToByteArray(), toSig);
                var reviewer = authorization.Reviewers.FirstOrDefault(r => r.PubKey.SequenceEqual(recovered));

                if (reviewer == null)
                    return false;
                    
                weight += reviewer.Weight;
                    
                return true;
            });

            //Api.Assert(validApprovals, "Unauthorized approval."); //This should never happen.
            //Api.Assert(weight >= authorization.ExecutionThreshold, "Not enough approvals.");
            return validApprovals && weight >= authorization.ExecutionThreshold;
        }

        private void CheckSignature(byte[] txnData, byte[] approvalSignature)
        {
            var proposedTxn = GetPendingTxn(txnData);
            proposedTxn.Sigs.Add(ByteString.CopyFrom(approvalSignature));
            Api.Assert(Api.VerifySignature(proposedTxn), "Incorrect signature");
        }

        private void CheckTxnData(Address msigAddress, byte[] txnData)
        {
            var proposedTxn = GetPendingTxn(txnData);
            Api.Assert(proposedTxn.From.Equals(msigAddress),
                "From address in proposed transaction is not valid multisig account.");
            Api.Assert(proposedTxn.Sigs.Count == 0, "Invalid signatures in proposed transaction.");
            //Api.Assert(proposedTxn.Type == TransactionType.MsigTransaction, "Incorrect proposed transaction type.");
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