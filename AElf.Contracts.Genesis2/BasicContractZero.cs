using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using Google.Protobuf;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZero : CSharpSmartContract<BasicContractZeroState>, ISmartContractZero
    {
        private const double DeployWaitingPeriod = 3 * 24 * 60 * 60;
        
        #region Views
        
        [View]
        public ulong CurrentContractSerialNumber()
        {
            return State.ContractSerialNumber.Value;
        }
        
        [View]
        public string GetContractInfo(Address contractAddress)
        {
            var info = State.ContractInfos[contractAddress];
            if (info == null)
            {
                return string.Empty;
            }

            return info.ToString();
        }
        
        [View]
        public Address GetContractOwner(Address contractAddress)
        {
            var info = State.ContractInfos[contractAddress];
            return info?.Owner;
        }
        
        [View]
        public Hash GetContractHash(Address contractAddress)
        {
            var info = State.ContractInfos[contractAddress];
            return info?.ContractHash;
        }
        
        #endregion Views
        
        #region Actions
        
        public void Initialize(Address authorizationContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.AuthorizationContract.Value = authorizationContractAddress;
            State.Initialized.Value = true;
        }

        public byte[] InitSmartContract(ulong serialNumber, int category, byte[] code)
        {
            Assert(Context.CurrentHeight < 1, "The current height should be less than 1.");
            Assert(Context.Sender.Equals(Context.Genesis));
            
            var contractAddress = Address.BuildContractAddress(Context.ChainId, serialNumber);
            
            var contractHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Category = category,
                Owner = Address.Genesis,
                ContractHash = contractHash
            };
            State.ContractInfos[contractAddress] = info;
            
            var reg = new SmartContractRegistration
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                CodeHash = contractHash
            };
            
            Context.DeployContract(contractAddress, reg);

            Console.WriteLine("InitSmartContract - Deployment success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public byte[] DeploySmartContract(int category, byte[] code)
        {
            var serialNumber = GetNexSerialNumber();
            var contractAddress = Address.BuildContractAddress(Context.ChainId, serialNumber);
            
            var codeHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Category = category,
                Owner = Context.Sender,
                ContractHash = codeHash
            };
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                CodeHash = codeHash
            };

            Context.DeployContract(contractAddress, reg);

            Context.FireEvent(new ContractHasBeenDeployed()
            {
                CodeHash = codeHash,
                Address = contractAddress,
            });
            
            Console.WriteLine("BasicContractZero - Deployment ContractHash: " + codeHash.ToHex());
            Console.WriteLine("BasicContractZero - Deployment success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public byte[] UpdateSmartContract(Address contractAddress, byte[] code)
        {
            var existContract = State.ContractInfos[contractAddress];

            Assert(existContract != null, "Contract is not exist.");

            var isMultiSigAccount = IsMultiSigAccount(existContract.Owner);
            if (!existContract.Owner.Equals(Context.Sender))
            {
                Assert(isMultiSigAccount, "no permission.");
                var proposeHash = Propose("UpdateSmartContract", DeployWaitingPeriod, existContract.Owner,
                    Context.Self, "UpdateSmartContract", contractAddress, code);
                return proposeHash.DumpByteArray();
            }

            if (isMultiSigAccount)
            {
                CheckAuthority(Context.Sender);
            }

            var contractHash = Hash.FromRawBytes(code);
            Assert(!existContract.ContractHash.Equals(contractHash), "Contract is not changed.");

            existContract.ContractHash = contractHash;
            State.ContractInfos[contractAddress] = existContract;

            var reg = new SmartContractRegistration
            {
                Category = existContract.Category,
                Code = ByteString.CopyFrom(code),
                CodeHash = contractHash
            };

            Context.UpdateContract(contractAddress, reg);
            
            Console.WriteLine("BasicContractZero - update success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public void ChangeContractOwner(Address contractAddress, Address newOwner)
        {
            var info = State.ContractInfos[contractAddress];
            Assert(info != null && info.Owner.Equals(Context.Sender), "no permission.");
            
            var oldOwner = info.Owner;
            info.Owner = newOwner;
            State.ContractInfos[contractAddress] = info;
            Context.FireEvent(new OwnerHasBeenChanged
            {
                Address = contractAddress,
                OldOwner = oldOwner,
                NewOwner = newOwner
            });
        }

        #endregion Actions
    }
}