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
    #region Events

    public class ContractHasBeenDeployed : Event
    {
        [Indexed] public Hash Creator;

        [Indexed] public Hash Address;
    }

    public class OwnerHasBeenChanged : Event
    {
        [Indexed] public Hash Address;
        [Indexed] public Hash OldOwner;
        [Indexed] public Hash NewOwner;
    }
    
    #endregion Events

    #region Customized Field Types

    internal class SerialNumber : UInt64Field
    {
        internal static SerialNumber Instance { get; } = new SerialNumber();

        private SerialNumber() : this("__SerialNumber__")
        {
        }

        private SerialNumber(string name) : base(name)
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

        public SerialNumber Increment()
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

        private readonly SerialNumber _serialNumber = SerialNumber.Instance;
        private readonly Map<Hash, ContractInfo> _contractInfos = new Map<Hash, ContractInfo>("__contractInfos__");

        #endregion Fields

        public async Task<byte[]> DeploySmartContract(int category, byte[] code)
        {
            ulong serialNumber = _serialNumber.Increment().Value;

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
                Address = address
            }.Fire();
            */

            Console.WriteLine("Deployment success: " + address.ToHex());
            return address.GetHashBytes();
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

        public String TestInvoking(string str)
        {
            Console.WriteLine(str);
            return "Success";
        }
    }
}