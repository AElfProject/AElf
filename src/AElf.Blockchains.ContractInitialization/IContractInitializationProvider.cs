using System.Collections.Generic;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.ContractInitialization
{
    public interface IContractInitializationProvider
    {
        GenesisSmartContractDto GetGenesisSmartContractDto(IReadOnlyDictionary<string, byte[]> contractCodes);
    }
}