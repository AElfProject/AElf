using System;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Acs0;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZero : BasicContractZeroContainer.BasicContractZeroBase
    {
        #region Views

        public override UInt64Value CurrentContractSerialNumber(Empty input)
        {
            return new UInt64Value() {Value = State.ContractSerialNumber.Value};
        }

        public override ContractInfo GetContractInfo(Address input)
        {
            var info = State.ContractInfos[input];
            if (info == null)
            {
                return new ContractInfo();
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
            RequireAuthority(Context.Self);
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
            // hash name is needed when deploy contract
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


            State.NameAddressMapping[name] = contractAddress;
            return contractAddress;
        }

        public override Address DeploySmartContract(ContractDeploymentInput input)
        {
            RequireAuthority();
            var address = PrivateDeploySystemSmartContract(input.Name, input.Category, input.Code.ToByteArray());
            return address;
        }

        public override Address UpdateSmartContract(ContractUpdateInput input)
        {
            var contractAddress = input.Address;
            var code = input.Code.ToByteArray();
            var info = State.ContractInfos[contractAddress];

            Assert(info != null, "Contract does not exist.");
            RequireAuthority(info.Owner);

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
            var info = State.ContractInfos[contractAddress];
            Assert(info != null && info.Owner.Equals(Context.Sender), "no permission.");

            var oldOwner = info.Owner;
            info.Owner = input.NewOwner;
            State.ContractInfos[contractAddress] = info;
            var newOwner = input.NewOwner;
            Context.Fire(new OwnerChanged
            {
                Address = contractAddress,
                OldOwner = oldOwner,
                NewOwner = newOwner
            });
            return new Empty();
        }

        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Contract zero already initialized.");
            Assert(Context.Sender.Equals(Context.Self), "Unable to initialize.");
            State.ContractDeploymentAuthorityRequired.Value = input.ContractDeploymentAuthorityRequired;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty InitializeGenesisOwner(InitializeGenesisOwnerInput input)
        {
            Assert(State.GenesisOwner.Value == null, "Genesis owner already initialized");
            var address = GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            Assert(Context.Sender.Equals(address), "Unauthorized to initialize genesis contract.");
            Assert(input.GenesisOwner != null, "Genesis Owner should not be null."); 
            State.GenesisOwner.Value = input.GenesisOwner;
            return new Empty();
        }

        public override Empty ChangeGenesisOwner(Address newOwnerAddress)
        {
            Assert(State.Initialized.Value, "Contract zero not initialized.");
            Assert(State.GenesisOwner.Value != null, "Genesis owner not initialized");
            RequireAuthority(Context.Self);
            State.GenesisOwner.Value = newOwnerAddress;
            return new Empty();
        }

        #endregion Actions
        
        private void RequireAuthority(Address ownerAddress)
        {
            var sender = Context.Sender;
            var self = Context.Self;
            var contractDeploymentAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            var genesisOwner = State.GenesisOwner.Value;
            
            if (!State.Initialized.Value)
            {
                // only contract zero is permitted before initialization
                Assert(sender.Equals(self), "Unauthorized to do this."); 
            }
            else if (ownerAddress.Equals(Context.Self) || contractDeploymentAuthorityRequired)
            {
                // if it is deployed by contract zero
                // or if ContractDeploymentAuthorityRequired is true
                Assert(genesisOwner != null && sender.Equals(genesisOwner), "Unauthorized to do this.");
            }
            else
            {
                // otherwise only check original owner authority
                Assert(sender.Equals(ownerAddress));
            }
            
//            if (genesisOwner != null)
//            {
//                // if it is deployed by contract zero
//                // or if ContractDeploymentAuthorityRequired is true
//                if (ownerAddress.Equals(Context.Self) || contractDeploymentAuthorityRequired)
//                {
//                    // check GenesisOwner authority
//                    Assert(sender.Equals(genesisOwner), "Unauthorized to do this.");
//                }
//                else
//                {
//                    // otherwise only check original owner authority
//                    Assert(sender.Equals(ownerAddress));
//                }
//            }
//            else
//                Assert(sender.Equals(self), "Unauthorized to do this."); // only contract zero deploys system contracts before initialized
        }

        private void RequireAuthority()
        {
            var sender = Context.Sender;
            var self = Context.Self;
            var genesisOwner = State.GenesisOwner.Value;
            var contractDeploymentAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            if (!State.Initialized.Value)
            {
                // only contract zero is permitted before initialization
                Assert(sender.Equals(self), "Unauthorized to do this."); 
            }
            else if (contractDeploymentAuthorityRequired)
            {
                // genesis owner authority check is required
                Assert(genesisOwner != null && sender.Equals(genesisOwner), "Unauthorized to do this.");
            }
//            else if (genesisOwner == null)
//            {
//                Assert(sender.Equals(self), "Unauthorized to do this."); // only contract zero is permitted before genesis owner set
////                if (genesisOwner != null)
////                {
////                    // check authority if already initialized.
////                    Assert(sender.Equals(genesisOwner), "Unauthorized to do this.");
////                }
////                else
////                    Assert(sender.Equals(self), "Unauthorized to do this."); // only contract zero is permitted before genesis owner set
//             }
        }

//        private Address GetContractZeroOwner()
//        {
//            if (State.GenesisContractOwner.Value != null)
//                return State.GenesisContractOwner.Value;
//            
//            var address = GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
//            var contractZeroOwnerAddress =
//                Context.Call<Address>(address, State.ZeroOwnerAddressGenerationMethodName.Value, new Empty());
//            Assert(contractZeroOwnerAddress != null, "Invalid owner.");
//            State.GenesisContractOwner.Value = contractZeroOwnerAddress;
//            return contractZeroOwnerAddress;
//        }
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
            return Address.FromBytes(hash.DumpByteArray());
        }

        public static Address BuildContractAddress(int chainId, ulong serialNumber)
        {
            return BuildContractAddress(chainId.ComputeHash(), serialNumber);
        }
    }
}