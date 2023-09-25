using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Genesis
{
    public static class Extensions
    {
        public static SmartContractRegistration CreateNewSmartContractRegistration(int category, byte[] code)
        {
            var codeHash = HashHelper.ComputeFrom(code);
            return new SmartContractRegistration
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                CodeHash = codeHash
            };
        }

        public static SmartContractRegistration SetIsUserContract(this SmartContractRegistration self,
            bool isUserContract)
        {
            self.IsSystemContract = isUserContract;
            return self;
        }

        public static SmartContractRegistration SetVersion(this SmartContractRegistration self, int version)
        {
            self.Version = version;
            return self;
        }

        public static SmartContractRegistration SetContractVersion(this SmartContractRegistration self,
            string contractVersion)
        {
            self.ContractVersion = contractVersion;
            return self;
        }

        public static SmartContractRegistration SetIsSystemContract(this SmartContractRegistration self,
            bool isSystemContract)
        {
            self.IsSystemContract = isSystemContract;
            return self;
        }

        public static ContractInfo CreateContractInfo(SmartContractRegistration reg, Address author)
        {
            return new ContractInfo
            {
                Author = author,
                Category = reg.Category,
                CodeHash = reg.CodeHash,
                IsSystemContract = reg.IsSystemContract,
                Version = reg.Version,
                IsUserContract = reg.IsUserContract
            };
        }

        public static ContractInfo SetSerialNumber(this ContractInfo self, long serialNumber)
        {
            self.SerialNumber = serialNumber;
            return self;
        }

        public static ContractInfo SetContractVersion(this ContractInfo self, string contractVersion)
        {
            self.ContractVersion = contractVersion;
            return self;
        }

        public static ContractDeployed CreateContractDeployedEvent(SmartContractRegistration reg, Address contractAddress)
        {
            return new ContractDeployed
            {
                CodeHash = reg.CodeHash,
                Version = reg.Version,
                Address = contractAddress,
                ContractVersion = reg.ContractVersion
            };
        }

        public static ContractDeployed SetAuthor(this ContractDeployed self, Address author)
        {
            self.Author = author;
            return self;
        }

        public static ContractDeployed SetDeployer(this ContractDeployed self, Address deployer)
        {
            self.Deployer = deployer;
            return self;
        }

        public static ContractDeployed SetName(this ContractDeployed self, Hash name)
        {
            self.Name = name;
            return self;
        }

        public static bool RequiresDeterministicAddress(this ContractDeploymentInput self)
        {
            if (self.ContractOperation == null)
            {
                return false;
            }

            if (self.ContractOperation.Salt == null)
            {
                return false;
            }

            return self.ContractOperation.Deployer != null;
        }
    }
}