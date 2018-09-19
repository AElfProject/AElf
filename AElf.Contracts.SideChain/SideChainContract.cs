using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.SideChain
{
    
    #region Field Names

    public static class FieldNames
    {
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string SideChainInfos = "__SideChainInfos__";
        public static readonly string ParentChainBlockInfo = "__ParentBlockInfo__";
        public static readonly string HeightToParentChainHeight = "__HeightToParentChainHeight__";
    }

    #endregion Field Names

    #region Events

    public class SideChainCreationRequested : Event
    {
        public Hash Creator;
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

        private readonly Map<UInt64Value, ParentChainBlockRootInfo> _parentChainBlockInfo =
            new Map<UInt64Value, ParentChainBlockRootInfo>(FieldNames.ParentChainBlockInfo);
        
        // record self height 
        private readonly Map<UInt64Value, UInt64Value> _heightToParentChainHeight =
            new Map<UInt64Value, UInt64Value>(FieldNames.HeightToParentChainHeight);
        
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
            return info.LockedAddress.GetHashBytes();
        }

        #region Actions
        public byte[] CreateSideChain(Hash chainId, Hash lockedAddress, ulong lockedToken)
        {
            ulong serialNumber = _sideChainSerialNumber.Increment().Value;
            var info = new SideChainInfo()
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
            var indexedBlockHeight = parentChainBlockInfo.IndexedBlockHeight;
            foreach (var height in indexedBlockHeight)
            {
                // the parent height in which this side chain block was indexed
                var indexedHeight = _heightToParentChainHeight.GetValue(new UInt64Value {Value = height});
                Api.Assert(indexedHeight == null,
                    $"Height {height} has already been indexed by parent chain at height {indexedHeight.Value}");
            }
            
            ulong parentChainHeight = parentChainBlockInfo.Height;
            var key = new UInt64Value {Value = parentChainHeight};
            Api.Assert(_parentChainBlockInfo.GetValue(key) == null,
                $"Already written parent chain block info at height {parentChainHeight}");

            _parentChainBlockInfo[key] = new ParentChainBlockRootInfo
            {
                ChainId = parentChainBlockInfo.ChainId,
                Height = parentChainHeight,
                SideChainTransactionsRoot = parentChainBlockInfo.SideChainTransactionsRoot,
                SideChainBlockHeadersRoot = parentChainBlockInfo.SideChainBlockHeadersRoot
            };

            foreach (var height in indexedBlockHeight)
            {
                _heightToParentChainHeight[new UInt64Value {Value = height}] = key;
            }
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