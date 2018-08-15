using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Genesis
{
    #region Field Names

    public static class FieldNames
    {
        public static readonly string ContractSerialNumber = "__ContractSerialNumber__";
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string ContractInfos = "__ContractInfos__";
        public static readonly string SideChainInfos = "__sideChainInfos__";
    }

    #endregion Field Names

    #region Events

    public class ContractHasBeenDeployed : Event
    {
        [Indexed] public Hash Creator;
        [Indexed] public Hash Address;
        [Indexed] public Hash CodeHash;
    }

    public class SideChainCreationRequested : Event
    {
        public Hash Creator;
        public Hash ChainId;
    }

    public class SideChainCreationRequestApproved : Event
    {
        public SideChainInfo Info;
    }

    public class OwnerHasBeenChanged : Event
    {
        [Indexed] public Hash Address;
        [Indexed] public Hash OldOwner;
        [Indexed] public Hash NewOwner;
    }

    #endregion Events

    #region Customized Field Types

    internal class ContractSerialNumber : UInt64Field
    {
        internal static ContractSerialNumber Instance { get; } = new ContractSerialNumber();

        private ContractSerialNumber() : this(FieldNames.ContractSerialNumber)
        {
        }

        private ContractSerialNumber(string name) : base(name)
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

        public ContractSerialNumber Increment()
        {
            this.Value = this.Value + 1;
            SetValue(this.Value);
            return this;
        }
    }

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

    public class BasicContractZero : CSharpSmartContract, ISmartContractZero
    {
        #region Fields

        private readonly ContractSerialNumber _contractSerialNumber = ContractSerialNumber.Instance;
        private readonly SideChainSerialNumber _sideChainSerialNumber = SideChainSerialNumber.Instance;
        private readonly Map<Hash, ContractInfo> _contractInfos = new Map<Hash, ContractInfo>(FieldNames.ContractInfos);

        private readonly Map<Hash, SideChainInfo> _sideChainInfos =
            new Map<Hash, SideChainInfo>(FieldNames.SideChainInfos);

        #endregion Fields

        [View]
        public ulong CurrentContractSerialNumber()
        {
            return _contractSerialNumber.Value;
        }

        [View]
        public ulong CurrentSideChainSerialNumber()
        {
            return _sideChainSerialNumber.Value;
        }

        [View]
        public string GetContractInfoFor(Hash contractAddress)
        {
            var info = _contractInfos[contractAddress];
            if (info == null)
            {
                return String.Empty;
            }

            return info.ToString();
        }

        [View]
        public string GetSideChainInfoFor(Hash chainId)
        {
            var info = _sideChainInfos[chainId];
            if (info == null)
            {
                return String.Empty;
            }

            return info.ToString();
        }

        public async Task<byte[]> DeploySmartContract(int category, byte[] code)
        {
            ulong serialNumber = _contractSerialNumber.Increment().Value;

            Hash creator = Api.GetTransaction().From;

            var info = new ContractInfo()
            {
                Owner = creator,
                SerialNumer = serialNumber
            };

            var address = info.Address;

            _contractInfos[address] = info;

            SmartContractRegistration reg = new SmartContractRegistration()
            {
                Category = category,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = SHA256.Create().ComputeHash(code)
            };

            await Api.DeployContractAsync(address, reg);

            /*
            // TODO: Enable back
            // This is a quick fix, see https://github.com/AElfProject/AElf/issues/377
            new ContractHasBeenDeployed()
            {
                Creator = creator,
                Address = address,
                CodeHash = SHA256.Create().ComputeHash(code)
            }.Fire();
            */

            Console.WriteLine("Deployment success: " + address.ToHex());
            return address.GetHashBytes();
        }

        public byte[] CreateSideChain(int placeholder)
        {
            ulong serialNumber = _sideChainSerialNumber.Increment().Value;
            var chainId = Hash.Generate();
            var info = new SideChainInfo()
            {
                Owner = Api.GetTransaction().From,
                ChainId = chainId,
                SerialNumer = serialNumber,
                Status = SideChainStatus.Pending
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

        public void ChangeContractOwner(Hash contractAddress, Hash newOwner)
        {
            var info = _contractInfos[contractAddress];
            Api.Assert(info.Owner.Equals(Api.GetTransaction().From));
            var oldOwner = info.Owner;
            info.Owner = newOwner;
            _contractInfos[contractAddress] = info;
            new OwnerHasBeenChanged()
            {
                Address = contractAddress,
                OldOwner = oldOwner,
                NewOwner = newOwner
            }.Fire();
        }

        public Hash GetContractOwner(Hash contractAddress)
        {
            var info = _contractInfos[contractAddress];
            return info.Owner;
        }

        public String TestInvoking(byte[] str)
        {
            Console.WriteLine(str.ToHex());
            return "Success";
        }
    }
}