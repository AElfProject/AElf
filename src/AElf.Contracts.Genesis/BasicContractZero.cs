using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZero : BasicContractZeroContainer.BasicContractZeroBase, ISmartContractZero
    {
        #region Views

        public override UInt64Value CurrentContractSerialNumber(Empty input)
        {
            return new UInt64Value() {Value = State.ContractSerialNumber.Value};
        }

        public override Kernel.ContractInfo GetContractInfo(Address input)
        {
            var info = State.ContractInfos[input];
            if (info == null)
            {
                return new Kernel.ContractInfo();
            }

            return info;
        }

        public override Address GetContractOwner(Address input)
        {
            var info = State.ContractInfos[input];
            return info?.Owner;
        }

        public override Hash GetContractHash(Address input)
        {
            var info = State.ContractInfos[input];
            return info?.CodeHash;
        }

        public override Address GetContractAddressByName(Hash input)
        {
            return State.NameAddressMapping[input];
        }

        public override SmartContractRegistration GetSmartContractRegistrationByAddress(Address input)
        {
            var info = State.ContractInfos[input];
            if (info == null)
            {
                return null;
            }

            return State.SmartContractRegistrations[info.CodeHash];
        }

        #endregion Views

        #region Actions

        public override Address DeploySystemSmartContract(SystemContractDeploymentInput input)
        {
            var name = input.Name;
            var category = input.Category;
            var code = input.Code.ToByteArray();
            var transactionMethodCallList = input.TransactionMethodCallList;
            var address = PrivateDeploySystemSmartContract(name, category, code);

            foreach (var methodCall in transactionMethodCallList.Value)
            {
                Context.SendInline(address, methodCall.MethodName, methodCall.Params);
            }

            return address;
        }


        private Address PrivateDeploySystemSmartContract(Hash name, int category, byte[] code)
        {
            if (name != null)
                Assert(State.NameAddressMapping[name] == null, "contract name already been registered");


            var serialNumber = State.ContractSerialNumber.Value;
            // Increment
            State.ContractSerialNumber.Value = serialNumber + 1;
            var contractAddress = AddressHelper.BuildContractAddress(Context.ChainId, serialNumber);

            var codeHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Owner = Context.Sender,
                Category = category,
                CodeHash = codeHash
            };
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                CodeHash = codeHash
            };

            State.SmartContractRegistrations[reg.CodeHash] = reg;

            Context.DeployContract(contractAddress, reg, name);

            Context.Fire(new ContractDeployed()
            {
                CodeHash = codeHash,
                Address = contractAddress,
                Creator = Context.Sender
            });

            Context.LogDebug(() => "BasicContractZero - Deployment ContractHash: " + codeHash.ToHex());
            Context.LogDebug(() => "BasicContractZero - Deployment success: " + contractAddress.GetFormatted());


            if (name != null)
                State.NameAddressMapping[name] = contractAddress;


            return contractAddress;
        }

        public override Address DeploySmartContract(ContractDeploymentInput input)
        {
            return DeploySystemSmartContract(new SystemContractDeploymentInput()
            {
                Category = input.Category,
                Code = input.Code,
                TransactionMethodCallList = new SystemTransactionMethodCallList()
            });
        }


        public override Address UpdateSmartContract(ContractUpdateInput input)
        {
            var contractAddress = input.Address;
            var code = input.Code.ToByteArray();
            var info = State.ContractInfos[contractAddress];

            Assert(info != null, "Contract does not exist.");
            Assert(info.Owner.Equals(Context.Sender), "Only owner is allowed to update code.");

            var oldCodeHash = info.CodeHash;
            var newCodeHash = Hash.FromRawBytes(code);
            Assert(!oldCodeHash.Equals(newCodeHash), "Code is not changed.");

            info.CodeHash = newCodeHash;
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = info.Category,
                Code = ByteString.CopyFrom(code),
                CodeHash = newCodeHash
            };

            State.SmartContractRegistrations[reg.CodeHash] = reg;

            Context.UpdateContract(contractAddress, reg, null);

            Context.Fire(new CodeUpdated()
            {
                Address = contractAddress,
                OldCodeHash = oldCodeHash,
                NewCodeHash = newCodeHash
            });

            Context.LogDebug(() => "BasicContractZero - update success: " + contractAddress.GetFormatted());
            return contractAddress;
        }

        public override Empty ChangeContractOwner(ChangeContractOwnerInput input)
        {
            var contractAddress = input.ContractAddress;
            var newOwner = input.NewOwner;
            var info = State.ContractInfos[contractAddress];
            Assert(info != null && info.Owner.Equals(Context.Sender), "no permission.");

            var oldOwner = info.Owner;
            info.Owner = input.NewOwner;
            State.ContractInfos[contractAddress] = info;
            Context.Fire(new OwnerChanged
            {
                Address = contractAddress,
                OldOwner = oldOwner,
                NewOwner = newOwner
            });
            return new Empty();
        }

        #endregion Actions
    }

    public static class AddressHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Address BuildContractAddress(Hash chainId, ulong serialNumber)
        {
            var hash = Hash.FromTwoHashes(chainId, Hash.FromRawBytes(serialNumber.ToBytes()));
            return Address.FromBytes(Address.TakeByAddressLength(hash.DumpByteArray()));
        }

        public static Address BuildContractAddress(int chainId, ulong serialNumber)
        {
            return BuildContractAddress(chainId.ComputeHash(), serialNumber);
        }
    }
}