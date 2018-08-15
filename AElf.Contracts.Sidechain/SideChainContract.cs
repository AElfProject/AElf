using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;

namespace AElf.Contracts.SideChain
{
    
    #region Field Names

    public static class FieldNames
    {
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string SideChainInfos = "__sideChainInfos__";
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

        #endregion Fields
        
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
            new SideChainDisposal()
            {
                chainId = chainId
            }.Fire();
        }

    }
    
}