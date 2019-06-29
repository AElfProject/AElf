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
            return GetActualContractOwner(info);
        }

        public override Hash GetContractHash(Address input)
        {
            var info = State.ContractInfos[input];
            return info?.CodeHash;
        }

        public override Address GetGenesisOwner(Empty input)
        {
            return State.GenesisOwner.Value;
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
            RequireAuthority(true);
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
                Owner = Context.Origin,
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
                Creator = Context.Origin
            });

            Context.LogDebug(() => "BasicContractZero - Deployment ContractHash: " + codeHash.ToHex());
            Context.LogDebug(() => "BasicContractZero - Deployment success: " + contractAddress.GetFormatted());


            if (name != null)
                State.NameAddressMapping[name] = contractAddress;


            return contractAddress;
        }

        public override Address DeploySmartContract(ContractDeploymentInput input)
        {
            var contractDeploymentAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            RequireAuthority(contractDeploymentAuthorityRequired);
            var address = PrivateDeploySystemSmartContract(null, input.Category, input.Code.ToByteArray());
            return address;
        }

        public override Address UpdateSmartContract(ContractUpdateInput input)
        {
            var contractAddress = input.Address;
            var code = input.Code.ToByteArray();
            var info = State.ContractInfos[contractAddress];

            Assert(info != null, "Contract does not exist.");
            var contractDeploymentAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;

            var ownerAddress = GetActualContractOwner(info);
            RequireAuthority(ownerAddress);

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

        public override Empty ChangeGenesisOwner(Address newOwnerAddress)
        {
            if (State.GenesisOwner.Value == null)
                InitializeGenesisOwner(newOwnerAddress);
            else
            {
                RequireAuthority(true);
                State.GenesisOwner.Value = newOwnerAddress;
            }

            return new Empty();
        }

        public override Empty ChangeAdministrator(Address newAdministrator)
        {
            if (State.Administrator.Value == null)
                InitializeAdministrator(newAdministrator);
            else
            {
                RequireAuthority(State.Administrator.Value);
                State.Administrator.Value = newAdministrator;
            }
            
            return new Empty();
        }

        #endregion Actions
        
        private void RequireAuthority(Address ownerAddress)
        {
            var authorityRequiredAddress = ownerAddress;
            if (!State.Initialized.Value)
            {
                // only authority of contract zero is valid before initialization 
                authorityRequiredAddress = Context.Self;
            }
            else if (ownerAddress.Equals(Context.Self))
            {
                // if it is owned by contract zero, require authority of genesis owner
                authorityRequiredAddress = State.GenesisOwner.Value;
            }
            
            CheckAuthorityWith(authorityRequiredAddress);
        }

        private void RequireAuthority(bool isGenesisOwnerAuthorityRequired)
        {
            Address authorityRequiredAddress;
            if (!State.Initialized.Value)
            {
                // only authority of contract zero is valid before initialization 
                authorityRequiredAddress = Context.Self;
            }
            else if (isGenesisOwnerAuthorityRequired)
            {
                // genesis owner authority check is required
                authorityRequiredAddress = State.GenesisOwner.Value;
            }
            else
            {
                // administrator authority check is required if it is not null.
                authorityRequiredAddress = State.Administrator.Value;
            }
            
            if (authorityRequiredAddress != null)
                CheckAuthorityWith(authorityRequiredAddress);
        }

        private void CheckAuthorityWith(Address address)
        {
            Assert(Context.Sender.Equals(address), "Unauthorized behavior.");
        }
        
        private void InitializeGenesisOwner(Address genesisOwner)
        {
            Assert(State.GenesisOwner.Value == null, "Genesis owner already initialized");
            var address = GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            Assert(Context.Sender.Equals(address), "Unauthorized to initialize genesis contract.");
            Assert(genesisOwner != null, "Genesis Owner should not be null."); 
            State.GenesisOwner.Value = genesisOwner;
        }

        private void InitializeAdministrator(Address administrator)
        {
            Assert(State.Administrator.Value == null, "Administrator already initialized");
            var address = GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            Assert(Context.Sender.Equals(address), "Unauthorized to initialize genesis contract.");
            Assert(administrator != null, "Genesis Owner should not be null."); 
            State.GenesisOwner.Value = administrator;
        }

        private Address GetActualContractOwner(ContractInfo contractInfo)
        {
            var contractDeploymentAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            return contractInfo.Owner.Equals(Context.Self) || contractDeploymentAuthorityRequired
                ? Context.Self
                : contractInfo.Owner;
        }
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
            return BuildContractAddress(chainId.ToHash(), serialNumber);
        }
    }
}