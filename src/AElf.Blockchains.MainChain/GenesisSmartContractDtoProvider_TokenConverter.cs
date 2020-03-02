using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTokenConverter()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.TokenConverter"),
                TokenConverterSmartContractAddressNameProvider.Name,
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList());
            return l;
        }
    }
}