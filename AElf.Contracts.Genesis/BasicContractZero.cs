using System;
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
                _value = GetValue();

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

        private const double DeployWaitingPeriod = 3 * 24 * 60 * 60;

        #endregion Fields

        [View]
        public ulong CurrentContractSerialNumber()
        {
            return _contractSerialNumber.Value;
        }
        
        public string GetContractInfo(Address contractAddress)
        {
            var info = _contractInfos[contractAddress];
            if (info == null)
            {
                return string.Empty;
            }

            return info.ToString();
        }
        
        public async Task<byte[]> InitSmartContract(ulong serialNumber, int category, byte[] code)
        {
            Api.Assert(Api.GetCurrentHeight() < 1, "The current height should be less than 1.");
            
            var contractAddress = Address.BuildContractAddress(Api.ChainId.DumpByteArray(), serialNumber);
            
            var contractHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Category = category,
                Owner = Address.Genesis,
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
            
            await Api.InitContractAsync(contractAddress, reg);

            Console.WriteLine("InitSmartContract - Deployment success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public async Task<byte[]> DeploySmartContract(int category, byte[] code)
        {
            var serialNumber = _contractSerialNumber.Increment().Value;
            var contractAddress = Address.BuildContractAddress(Api.ChainId.DumpByteArray(), serialNumber);
            
            var creator = Api.GetFromAddress();
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

            Console.WriteLine("BasicContractZero - Deployment ContractHash: " + contractHash.ToHex());
            Console.WriteLine("BasicContractZero - Deployment success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public async Task<byte[]> UpdateSmartContract(Address contractAddress, byte[] code)
        {
            var isExistContract = _contractInfos.TryGet(contractAddress, out var existContract);

            Api.Assert(isExistContract, "Contract is not exist.");

            var fromAddress = Api.GetFromAddress();
            var isMultiSigAccount = Api.IsMultiSigAccount(existContract.Owner);
            if (!existContract.Owner.Equals(fromAddress))
            {
                Api.Assert(isMultiSigAccount, "no permission.");
                var proposeHash = Api.Propose("UpdateSmartContract", DeployWaitingPeriod, existContract.Owner,
                    Api.GetContractAddress(), "UpdateSmartContract", contractAddress, code);
                return proposeHash.DumpByteArray();
            }

            if (isMultiSigAccount)
            {
                Api.CheckAuthority(fromAddress);
            }

            var contractHash = Hash.FromRawBytes(code);
            Api.Assert(!existContract.ContractHash.Equals(contractHash), "Contract is not changed.");

            existContract.ContractHash = contractHash;
            _contractInfos.SetValue(contractAddress, existContract);

            var reg = new SmartContractRegistration
            {
                Category = existContract.Category,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = contractHash,
                SerialNumber = existContract.SerialNumber
            };

            await Api.UpdateContractAsync(contractAddress, reg);
            
            Console.WriteLine("BasicContractZero - update success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public void ChangeContractOwner(Address contractAddress, Address newOwner)
        {
            var info = _contractInfos[contractAddress];
            Api.Assert(info.Owner.Equals(Api.GetFromAddress()), "no permission.");
            
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

        [View]
        public Address GetContractOwner(Address contractAddress)
        {
            var info = _contractInfos[contractAddress];
            return info?.Owner;
        }
        
        [View]
        public Hash GetContractHash(Address contractAddress)
        {
            var info = _contractInfos[contractAddress];
            return info?.ContractHash;
        }
    }
}