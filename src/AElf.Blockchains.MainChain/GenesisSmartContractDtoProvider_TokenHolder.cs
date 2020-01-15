using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTokenHolder()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("TokenHolder")).Value,
                TokenHolderSmartContractAddressNameProvider.Name,
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList());
            return l;
        }
    }
}