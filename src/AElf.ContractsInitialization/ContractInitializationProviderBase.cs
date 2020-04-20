using Acs0;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.ContractsInitialization
{
    public abstract class ContractInitializationProviderBase : IContractInitializationProvider
    {
        public abstract Hash SmartContractName { get; }
        public abstract string ContractCodeName { get; }
        public int Tier => 0;

        public GenesisSmartContractDto GetGenesisSmartContractDto(byte[] contractCode)
        {
            return new GenesisSmartContractDto
            {
                Code = contractCode,
                SystemSmartContractName = SmartContractName,
                TransactionMethodCallList = GenerateInitializationCallList()
            };
        }

        protected virtual SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        }
    }
}