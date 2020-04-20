using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.ContractsInitialization
{
    public interface IContractInitializationProvider
    {
        public Hash SmartContractName { get; }
        public string ContractCodeName { get; }
        public int Tier { get; }
        GenesisSmartContractDto GetGenesisSmartContractDto(byte[] contractCode);
    }
}