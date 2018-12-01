using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Types.Proposal;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.CrossChain
{
    
    #region Field Names

    public static class FieldNames
    {
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string SideChainInfos = "__SideChainInfos__";
        public static readonly string ParentChainBlockInfo = GlobalConfig.AElfParentChainBlockInfo;
        public static readonly string AElfBoundParentChainHeight = GlobalConfig.AElfBoundParentChainHeight;
        public static readonly string TxRootMerklePathInParentChain = GlobalConfig.AElfTxRootMerklePathInParentChain;
        public static readonly string CurrentParentChainHeight = GlobalConfig.AElfCurrentParentChainHeight;
        public static readonly string NewChainCreationRequest = "_NewChainCreationRequest_";
    }

    #endregion Field Names

    #region Events

    public class SideChainCreationRequested : Event
    {
        public Address Creator;
        public Hash ChainId;
    }

    public class SideChainCreationRequestApproved : Event
    {
        public SideChainInfo Info;
    }

    public class SideChainDisposal : Event
    {
        public Hash chainId;
    }

    #endregion Events

    #region Customized Field Types

    internal class SideChainSerialNumber : UInt64Field
    {
        internal static SideChainSerialNumber Instance { get; } = new SideChainSerialNumber();

        private SideChainSerialNumber() : this(FieldNames.SideChainSerialNumber)
        {
        }

        private SideChainSerialNumber(string name) : base(name)
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

                return _value;
            }
            private set { _value = value; }
        }

        public SideChainSerialNumber Increment()
        {
            this.Value = this.Value + 1;
            SetValue(this.Value);
            return this;
        }
    }

    #endregion Customized Field Types

   
    public class CrossChainContract : CSharpSmartContract
    {
        #region Fields

        private readonly SideChainSerialNumber _sideChainSerialNumber = SideChainSerialNumber.Instance;
        private readonly Map<Hash, SideChainInfo> _sideChainInfos =
            new Map<Hash, SideChainInfo>(FieldNames.SideChainInfos);

        private readonly Map<UInt64Value, ParentChainBlockInfo> _parentChainBlockInfo =
            new Map<UInt64Value, ParentChainBlockInfo>(FieldNames.ParentChainBlockInfo);
        
        // record self height 
        private readonly Map<UInt64Value, UInt64Value> _childHeightToParentChainHeight =
            new Map<UInt64Value, UInt64Value>(FieldNames.AElfBoundParentChainHeight);

        private readonly Map<UInt64Value, MerklePath> _txRootMerklePathInParentChain =
            new Map<UInt64Value, MerklePath>(FieldNames.TxRootMerklePathInParentChain);

        private readonly UInt64Field _currentParentChainHeight = new UInt64Field(FieldNames.CurrentParentChainHeight);

        private readonly Map<Hash, ChainCreationRequest> _requestInfo =
            new Map<Hash, ChainCreationRequest>(FieldNames.NewChainCreationRequest);

        #endregion Fields

        private double ReuqestChainCreationWaitingPeriod { get; } = 24 * 60 * 60;

        [View]
        public ulong CurrentSideChainSerialNumber()
        {
            return _sideChainSerialNumber.Value;
        }

        public ulong LockedToken(Hash chainId)
        {
            Api.Assert(_sideChainInfos.GetValue(chainId) != null, "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            Api.Assert(info.Status != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedToken;
        }
        
        public byte[] LockedAddress(Hash chainId)
        {
            Api.Assert(_sideChainInfos.GetValue(chainId) != null, "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            Api.Assert(info.Status != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedAddress.DumpByteArray();
        }

        #region Actions
        
        public byte[] ReuqestChainCreation(ChainCreationRequest request)
        {
            Api.Assert(request.Proposer != null && Api.GetTransactionFromAddress().Equals(request.Proposer),
                "Invalid chain creation request.");

/*
            // Todo: temp chainId calculation method
            Hash chainId = Hash.Generate();
            
            Transaction createSideChainTxnData = new Transaction
            {
                From = Address.Genesis,
                To = Api.GetContractAddress(),
                MethodName = "CreateSideChain",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId, request)),
                Type = TransactionType.MsigTransaction
            };

            var txnData = new PendingTxn
            {
                ProposalName = "ChainCreation",
                TxnData = ByteString.CopyFrom(createSideChainTxnData.GetHash().DumpByteArray())
            };
            
            Proposal proposal = new Proposal
            {
                MultiSigAccount = Address.Genesis,
                Name = txnData.ProposalName,
                TxnData = txnData,
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(ReuqestChainCreationWaitingPeriod)),
                Status = ProposalStatus.ToBeDecided,
                Proposer = Api.GetTransactionFromAddress()
            };
*/
            

            Hash proposalHash = request.GetHash();

            _requestInfo.SetValue(proposalHash, request);
            return proposalHash.DumpByteArray();
        }

        public byte[] CreateSideChain(Hash chainId, ChainCreationRequest request)
        {
            ulong serialNumber = _sideChainSerialNumber.Increment().Value;
            var info = new SideChainInfo
            {
                Owner = Api.GetTransaction().From,
                ChainId = chainId,
                SerialNumer = serialNumber,
                Status = SideChainStatus.Pending,
                LockedAddress = request.Proposer,
                CreationHeight = Api.GetCurrentHeight() + 1 
            };
            _sideChainInfos[chainId] = info;
            new SideChainCreationRequested
            {
                ChainId = chainId,
                Creator = Api.GetTransaction().From
            }.Fire();
            return chainId.DumpByteArray();
        }
    
        public void ApproveSideChain(Hash chainId)
        {
            // TODO: Only privileged account can trigger this method
            var info = _sideChainInfos[chainId];
            Api.Assert(info != null, "Invalid chain id.");
            Api.Assert(info?.Status == SideChainStatus.Pending, "Invalid chain status.");
            info.Status = SideChainStatus.Active;
            _sideChainInfos[chainId] = info;
            new SideChainCreationRequestApproved()
            {
                Info = info.Clone()
            }.Fire();
        }
    
        public void DisposeSideChain(Hash chainId)
        {
            Api.Assert(_sideChainInfos.GetValue(chainId) != null, "Not existed side chain");
            // TODO: Only privileged account can trigger this method
            var info = _sideChainInfos[chainId];
            info.Status = SideChainStatus.Terminated;
            _sideChainInfos[chainId] = info;
            new SideChainDisposal
            {
                chainId = chainId
            }.Fire();
        }

        public void WriteParentChainBlockInfo(ParentChainBlockInfo parentChainBlockInfo)
        {
            ulong parentChainHeight = parentChainBlockInfo.Height;
            var currentHeight = _currentParentChainHeight.GetValue();
            var target = currentHeight != 0 ? currentHeight + 1: GlobalConfig.GenesisBlockHeight; 
            Api.Assert(target == parentChainHeight,
                $"Parent chain block info at height {target} is needed, not {parentChainHeight}");
            Console.WriteLine("ParentChainBlockInfo.Height is correct.");
            
            var key = new UInt64Value {Value = parentChainHeight};
            Api.Assert(_parentChainBlockInfo.GetValue(key).Equals(new ParentChainBlockInfo()),
                $"Already written parent chain block info at height {parentChainHeight}");
            Console.WriteLine("Writing ParentChainBlockInfo..");
            foreach (var _ in parentChainBlockInfo.IndexedBlockInfo)
            {
                BindParentChainHeight(_.Key, parentChainHeight);
                AddIndexedTxRootMerklePathInParentChain(_.Key, _.Value);
            }
            _parentChainBlockInfo.SetValueAsync(key, parentChainBlockInfo).Wait();
            _currentParentChainHeight.SetValue(parentChainHeight);
            
            // only for debug
            Console.WriteLine($"WriteParentChainBlockInfo success at {parentChainHeight}");
        }

        public bool VerifyTransaction(Hash tx, MerklePath path, ulong parentChainHeight)
        {
            var key = new UInt64Value {Value = parentChainHeight};
            Api.Assert(_parentChainBlockInfo.GetValue(key) != null,
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated =  path.ComputeRootWith(tx);
            var parentRoot = _parentChainBlockInfo.GetValue(key)?.Root?.SideChainTransactionsRoot;
            //Api.Assert((parentRoot??Hash.Zero).Equals(rootCalculated), "Transaction verification Failed");
            return (parentRoot??Hash.Zero).Equals(rootCalculated);
        }

        private void BindParentChainHeight(ulong childHeight, ulong parentHeight)
        {
            var key = new UInt64Value {Value = childHeight};
            Api.Assert(_childHeightToParentChainHeight.GetValue(key).Value == 0,
                $"Already bound at height {childHeight} with parent chain");
//            _childHeightToParentChainHeight[key] = new UInt64Value {Value = parentHeight};
            _childHeightToParentChainHeight.SetValueAsync(key, new UInt64Value{Value = parentHeight}).Wait();
        }

        private void AddIndexedTxRootMerklePathInParentChain(ulong height, MerklePath path)
        {
            var key = new UInt64Value {Value = height};
            Api.Assert(_txRootMerklePathInParentChain.GetValue(key).Equals(new MerklePath()),
                $"Merkle path already bound at height {height}.");
//            _txRootMerklePathInParentChain[key] = path;
            _txRootMerklePathInParentChain.SetValueAsync(key, path).Wait();
            Console.WriteLine("Path: {0}", path.Path[0].DumpHex());

        }
        #endregion
        

        public int GetChainStatus(Hash chainId)
        {
            Api.Assert(!_sideChainInfos.GetValue(chainId).Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            return (int) info.Status;
        } 
    }
}