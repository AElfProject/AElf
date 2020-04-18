using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public abstract class ContractInitializationProviderBase : IContractInitializationProvider
    {
        protected abstract Hash ContractName { get; }

        protected abstract string ContractCodeName { get; }

        public GenesisSmartContractDto GetGenesisSmartContractDto(
            IReadOnlyDictionary<string, byte[]> contractCodes)
        {
            return new GenesisSmartContractDto
            {
                Code = GetContractCodeByName(contractCodes, ContractCodeName),
                SystemSmartContractName = ContractName,
                TransactionMethodCallList = GenerateInitializationCallList()
            };
        }

        protected virtual SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        }

        protected byte[] GetContractCodeByName(IReadOnlyDictionary<string, byte[]> contractCodes, string name)
        {
            return contractCodes.Single(kv => kv.Key.Contains(name)).Value;
        }
    }
}