using System.Collections.Generic;
using AElf.Types;

namespace AElf.OS.Node.Application
{
    public interface IGenesisSmartContractDtoProvider
    {
        IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress);
    }
}