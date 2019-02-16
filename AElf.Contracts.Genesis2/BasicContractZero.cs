using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZero : CSharpSmartContract<BasicContractZeroState>
    {
        private const double DeployWaitingPeriod = 3 * 24 * 60 * 60;

        public void Initialize(Address consensusContractAddress, Address tokenContractAddress,
            Address authorizationContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.AuthorizationContract.Value = authorizationContractAddress;
            State.Initialized.Value = true;
        }
        
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

        public async Task<byte[]> InitSmartContract(ulong serialNumber, int category, byte[] code)
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
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = contractHash,
                SerialNumber = serialNumber
            };
            
            await Context.DeployContractAsync(contractAddress, reg);

            Console.WriteLine("InitSmartContract - Deployment success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public async Task<byte[]> DeploySmartContract(int category, byte[] code)
        {
            var serialNumber = GetNexSerialNumber();
            var contractAddress = Address.BuildContractAddress(Context.ChainId, serialNumber);
            
            var contractHash = Hash.FromRawBytes(code);

            var info = new ContractInfo
            {
                SerialNumber = serialNumber,
                Category = category,
                Owner = Context.Sender,
                ContractHash = contractHash
            };
            State.ContractInfos[contractAddress] = info;

            var reg = new SmartContractRegistration
            {
                Category = category,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = contractHash,
                SerialNumber = serialNumber
            };

            await Context.DeployContractAsync(contractAddress, reg);

            Console.WriteLine("BasicContractZero - Deployment ContractHash: " + contractHash.ToHex());
            Console.WriteLine("BasicContractZero - Deployment success: " + contractAddress.GetFormatted());
            return contractAddress.DumpByteArray();
        }

        public async Task<byte[]> UpdateSmartContract(Address contractAddress, byte[] code)
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
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = contractHash,
                SerialNumber = existContract.SerialNumber
            };

            await Context.UpdateContractAsync(contractAddress, reg);
            
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
            Context.FireEvent(new Events.OwnerHasBeenChanged
            {
                Address = contractAddress,
                OldOwner = oldOwner,
                NewOwner = newOwner
            });
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
    }
}