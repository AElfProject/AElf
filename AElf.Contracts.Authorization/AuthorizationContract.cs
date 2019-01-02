using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Timers;
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

        #region View

        [View]
        public Proposal GetProposal(Hash proposalHash)
        {
            TryGetProposal(proposalHash, out var proposal);
            return proposal;
        }

        #endregion view
        
        
        #region Actions

        public byte[] CreateMultiSigAccount(Kernel.Authorization authorization)
        {
            Api.Assert(authorization.Reviewers.Count > 0 && authorization.Reviewers.All(r => r.PubKey.Length > 0),
                "Invalid authorization for multi signature.");
            Address multiSigAccount = authorization.MultiSigAccount ??
                                      Address.FromPublicKey(authorization.ToByteArray().ToArray());
            Api.Assert(!_multiSig.TryGet(multiSigAccount, out _),"MultiSigAccount already existed.");
            uint accumulatedWeights =
                authorization.Reviewers.Aggregate<Reviewer, uint>(0, (weights, r) => weights + r.Weight);
            
            // Weight accumulation should be more than authorization execution threshold.
            Api.Assert(accumulatedWeights >= authorization.ExecutionThreshold, "Invalid authorization.");
            
            // At least one reviewer can propose.
            bool canBeProposed = authorization.Reviewers.Any(r => r.Weight >= authorization.ProposerThreshold);
            Api.Assert(canBeProposed, "Invalid authorization.");
            
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

            Api.Assert(Api.CurrentBlockTime < timestamp, "Expired proposal.");

            Hash hash = proposal.GetHash();
            Api.Assert(!_proposals.TryGet(hash, out _), "Proposal already created.");

            // check authorization of proposer public key
            var auth = GetAuthorization(proposal.MultiSigAccount);
            CheckAuthority(proposal, auth);
            SetProposal(hash, proposal);
            return hash.DumpByteArray();
        }

        public bool SayYes(Approval approval)
        {
            // check validity of proposal 
            Hash hash = approval.ProposalHash;

            // check approval not existed
            Api.Assert(!_approved.TryGet(hash, out var approved) || !approved.Approvals.Contains(approval),
                "Approval already existed.");

            // check authorization and permission 
            TryGetProposal(hash, out var proposal);
            Api.Assert(Api.CurrentBlockTime < TimerHelper.ConvertFromUnixTimestamp(proposal.ExpiredTime), "Expired proposal.");
            
            var msig = proposal.MultiSigAccount;
            var authorization = GetAuthorization(msig);
            byte[] toSig = proposal.TxnData.ToByteArray().CalculateHash();
            byte[] pubKey = Api.RecoverPublicKey(approval.Signature.ToByteArray(), toSig);
            Api.Assert(Api.RecoverPublicKey().SequenceEqual(pubKey), "Invalid approval.");
            Api.Assert(authorization.Reviewers.Any(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)), "Not authorized approval.");
            
            CheckSignature(proposal.TxnData.ToByteArray(), approval.Signature.ToByteArray());
            approved = approved ?? new Approved(); 
            approved.Approvals.Add(approval);
            _approved.SetValue(hash, approved);

            if (CheckPermission(approved, authorization, proposal))
            {
                // Executing threshold already reached.
                proposal.Status = ProposalStatus.Decided;
                SetProposal(hash, proposal);
            }
            
            return true;
        }

        public byte[] Release(Hash proposalHash)
        {
            TryGetProposal(proposalHash, out var proposal);
            // check expired time of proposal
            Api.Assert(Api.CurrentBlockTime < TimerHelper.ConvertFromUnixTimestamp(proposal.ExpiredTime),
                "Expired proposal.");
            Api.Assert(proposal.Status != ProposalStatus.Released, "Proposal already released");
            
            var msigAccount = proposal.MultiSigAccount;
            var auth = GetAuthorization(msigAccount);

            // check approvals
            var approved = _approved.GetValue(proposalHash);
            Api.Assert(proposal.Status == ProposalStatus.Decided && CheckPermission(approved, auth, proposal),
                "Proposal can not be released.");

            // check and append signatures to packed txn
            // check authorization of proposal
            var proposedTxn = CheckAndFillTxnData(proposal, approved);
            // send deferred transaction
            Api.SendDeferredTransaction(proposedTxn);
            proposal.Status = ProposalStatus.Released;
            SetProposal(proposalHash, proposal);
            return proposedTxn.GetHash().DumpByteArray();
        }

        #endregion


        private Kernel.Authorization GetAuthorization(Address address)
        {
            // case 1: get authorization of normal multi sig account
            if (!address.Equals(Genesis))
            {
                Api.Assert(_multiSig.TryGet(address, out var authorization), "MultiSigAccount not found.");
                return authorization;
            }
             
            // case 2: get authorization of system account  
            var reviewers = Api.GetMiners().PublicKeys;
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

        private void TryGetProposal(Hash proposalHash, out Proposal proposal)
        {
            Api.Assert(_proposals.TryGet(proposalHash, out proposal), "Proposal not found.");
        }

        private void SetProposal(Hash proposalHash, Proposal proposal)
        {
            _proposals.SetValue(proposalHash, proposal);
        }

        /*private bool GetAuthorization(Address address, out Authorization authorization)
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