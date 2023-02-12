using System.Collections.Generic;

namespace AElf.OS.Node.Application;

public interface IGenesisSmartContractDtoProvider
{
    IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos();
}