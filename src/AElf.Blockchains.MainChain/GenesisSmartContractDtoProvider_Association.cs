using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Kernel;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForAssociation()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("AssociationAuth")).Value,
                AssociationAuthSmartContractAddressNameProvider.Name,
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList());

            return l;
        }
    }
}