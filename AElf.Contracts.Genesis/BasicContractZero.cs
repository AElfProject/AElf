using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;

namespace AElf.Contracts.Genesis
{
    #region Field Names

    public static class FieldNames
    {
        public static readonly string ContractSerialNumber = "__ContractSerialNumber__";
        public static readonly string ContractInfos = "__ContractInfos__";
    }

    #endregion Field Names

    #region Events

    public class ContractHasBeenDeployed : Event
    {
        [Indexed] public Address Creator;
        [Indexed] public Address Address;
        [Indexed] public Hash CodeHash;
    }

    public class SideChainCreationRequested : Event
    {
        public Address Creator;
        public Hash ChainId;
    }

    public class OwnerHasBeenChanged : Event
    {
        [Indexed] public Address Address;
        [Indexed] public Address OldOwner;
        [Indexed] public Address NewOwner;
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

                if (GlobalConfig.BasicContractZeroSerialNumber > _value)
                {
                    _value = GlobalConfig.BasicContractZeroSerialNumber;
                }
                return _value;
            }
            private set => _value = value;
        }

        public ContractSerialNumber Increment()
        {
            Value = Value + 1;
            SetValue(Value);
            GlobalConfig.BasicContractZeroSerialNumber = Value;
            return this;
        }
    }

    #endregion Customized Field Types

    public class BasicContractZero : CSharpSmartContract, ISmartContractZero
    {
        #region Fields

        private readonly Map<Address, ContractInfo> _contractInfos = new Map<Address, ContractInfo>(FieldNames.ContractInfos);
        
        private readonly ContractSerialNumber _contractSerialNumber = ContractSerialNumber.Instance;

        #endregion Fields

        [View]
        public ulong CurrentContractSerialNumber()
        {
            return _contractSerialNumber.Value;
        }
        
        [View]
        public string GetContractInfoFor(Address contractAddress)
        {
            var info = _contractInfos[contractAddress];
            if (info == null)
            {
                return string.Empty;
            }

            return info.ToString();
        }

        public async Task<byte[]> DeploySmartContract(int category, byte[] code)
        {
            var serialNumber = _contractSerialNumber.Increment().Value;
            var contractAddress = Address.FromRawBytes(serialNumber.ToString().CalculateHash());
            
            var creator = Api.GetTransaction().From;
            var contractHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Category = category,
                Owner = creator,
                ContractHash = contractHash
            };
            _contractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = category,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = contractHash,
                SerialNumber = serialNumber
            };

            await Api.DeployContractAsync(contractAddress, reg);

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
            Console.WriteLine("BasicContractZero - Deployment ContractHash: " + contractHash.DumpHex());
            Console.WriteLine("BasicContractZero - Deployment success: " + contractAddress.DumpHex());
            return contractAddress.DumpByteArray();
        }

        public async Task<bool> UpdateSmartContract(Address contractAddress, byte[] code)
        {
            var isExistContract = _contractInfos.TryGet(contractAddress, out var existContract);
            
            Api.Assert(isExistContract,"Contract is not exist.");
            
            var contractHash = Hash.FromRawBytes(code);
            Api.Assert(!existContract.ContractHash.Equals(contractHash),"Contract is not changed.");

            existContract.ContractHash = contractHash;
            _contractInfos.SetValue(contractAddress, existContract);
            
            var reg = new SmartContractRegistration
            {
                Category = existContract.Category,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = contractHash
            };

            await Api.UpdateContractAsync(contractAddress, reg);
            
            return true;
        }

        public void ChangeContractOwner(Address contractAddress, Address newOwner)
        {
            var info = _contractInfos[contractAddress];
            Api.Assert(info.Owner.Equals(Api.GetTransaction().From));
            var oldOwner = info.Owner;
            info.Owner = newOwner;
            _contractInfos[contractAddress] = info;
            new OwnerHasBeenChanged
            {
                Address = contractAddress,
                OldOwner = oldOwner,
                NewOwner = newOwner
            }.Fire();
        }

        public Address GetContractOwner(Address contractAddress)
        {
            var info = _contractInfos[contractAddress];
            return info?.Owner;
        }
    }
}