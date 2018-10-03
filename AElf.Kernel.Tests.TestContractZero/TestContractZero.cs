using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Contracts.Genesis;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp.MetadataAttribute;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SharpRepository.Repository.Configuration;
using Api = AElf.Sdk.CSharp.Api;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;
using AElf.Kernel.KernelAccount;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Tests
{
    public class TestContractZero : CSharpSmartContract, ISmartContractZero
    {

        #region Events

        public class ContractHasBeenDeployed : Event
        {
            [Indexed] public Address Creator;

            [Indexed] public Address Address;
        }

        public class OwnerHasBeenChanged : Event
        {
            [Indexed] public Address Address;
            [Indexed] public Address OldOwner;
            [Indexed] public Address NewOwner;
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
        
        private readonly SerialNumber _serialNumber = SerialNumber.Instance;
        private readonly Map<Hash, ContractInfo> _contractInfos = new Map<Hash, ContractInfo>("__contractInfos__");
        
        [SmartContractFieldData("${this}._deployLock", DataAccessMode.ReadWriteAccountSharing)]
        private object _deployLock;        

        [SmartContractFunction("${this}.DeploySmartContract", new string[]{}, new string[]{"${this}._deployLock"})]
        public async Task<byte[]> DeploySmartContract(int category, byte[] contract)
        {
            SmartContractRegistration registration = new SmartContractRegistration
            {
                Category = category,
                ContractBytes = ByteString.CopyFrom(contract),
                ContractHash = Hash.FromBytes(contract)  
            };
            
            var tx = Api.GetTransaction();
            
            ulong serialNumber = _serialNumber.Increment().Value;

            var creator = Api.GetTransaction().From;

            var info = new ContractInfo()
            {
                Owner = creator,
                SerialNumer = serialNumber
            };

            var address = info.Address;
            // calculate new account address
            var account = DataPath.CalculateAccountAddress(tx.From, tx.IncrementId);
            
            await Api.DeployContractAsync(account, registration);
            Console.WriteLine("Deployment success, {0}", account.Dumps());
            return account.GetValueBytes();
        }

        public void Print(string name)
        {
            Console.WriteLine("Hello, " + name);
        }
    }
}
