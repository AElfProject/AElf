using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;
using Globals = AElf.Kernel.Globals;

namespace AElf.Contracts.SideChain
{
    
    #region Field Names

    public static class FieldNames
    {
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string SideChainInfos = "__SideChainInfos__";
        public static readonly string ParentChainBlockInfo = Globals.AElfParentChainBlockInfo;
        public static readonly string AElfBoundParentChainHeight = Globals.AElfBoundParentChainHeight;
        public static readonly string TxRootMerklePathInParentChain = Globals.AElfTxRootMerklePathInParentChain;
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

    
    public class SideChainContract : CSharpSmartContract
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
        
        #endregion Fields
        
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
            return info.LockedAddress.GetValueBytes();
        }

        #region Actions
        public byte[] CreateSideChain(Hash chainId, Address lockedAddress, ulong lockedToken)
        {
            ulong serialNumber = _sideChainSerialNumber.Increment().Value;
            var info = new SideChainInfo
            {
                Owner = Api.GetTransaction().From,
                ChainId = chainId,
                SerialNumer = serialNumber,
                Status = SideChainStatus.Pending,
                LockedAddress = lockedAddress,
                LockedToken = lockedToken,
                CreationHeight = Api.GetCurerntHeight() + 1 
            };
            _sideChainInfos[chainId] = info;
            new SideChainCreationRequested()
            {
                ChainId = chainId,
                Creator = Api.GetTransaction().From
            }.Fire();
            return chainId.GetHashBytes();
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
            var key = new UInt64Value {Value = parentChainHeight};
            Api.Assert(_parentChainBlockInfo.GetValue(key) == null,
                $"Already written parent chain block info at height {parentChainHeight}");
            foreach (var _ in parentChainBlockInfo.IndexedBlockInfo)
            {
                BindParentChainHeight(_.Key, parentChainHeight);
                AddIndexedTxRootMerklePathInParentChain(_.Key, _.Value);
            }
            _parentChainBlockInfo.SetValueToDatabaseAsync(key, parentChainBlockInfo).Wait();
            Console.WriteLine("WriteParentChainBlockInfo success.");
        }

        public bool VerifyTransaction(Hash tx, MerklePath path, ulong parentChainHeight)
        {
            var key = new UInt64Value {Value = parentChainHeight};
            Api.Assert(_parentChainBlockInfo.GetValue(key) != null,
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated =  path.ComputeRootWith(tx);
            var parentRoot = _parentChainBlockInfo.GetValue(key)?.Root?.SideChainTransactionsRoot;
            Api.Assert((parentRoot??Hash.Zero).Equals(rootCalculated), "Transaction verification Failed");
            return true;
        }

        private void BindParentChainHeight(ulong childHeight, ulong parentHeight)
        {
            var key = new UInt64Value {Value = childHeight};
            Api.Assert(_childHeightToParentChainHeight.GetValue(key) == null,
                $"Already bound at height {childHeight} with parent chain");
            _childHeightToParentChainHeight.SetValueToDatabaseAsync(key, new UInt64Value{Value = parentHeight}).Wait();
        }

        private void AddIndexedTxRootMerklePathInParentChain(ulong height, MerklePath path)
        {
            var key = new UInt64Value {Value = height};
            Api.Assert(_txRootMerklePathInParentChain.GetValue(key) == null,
                $"Merkle path already bound at height {height}.");
            _txRootMerklePathInParentChain.SetValueToDatabaseAsync(key, path).Wait();
            Console.WriteLine("Path: {0}", path.Path[0].Dumps());

        }
        #endregion
        

        public int GetChainStatus(Hash chainId)
        {
            Api.Assert(_sideChainInfos.GetValue(chainId) != null, "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            return (int) info.Status;
        } 
    }
}