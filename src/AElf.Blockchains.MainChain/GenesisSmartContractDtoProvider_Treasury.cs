using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Treasury;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTreasury()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("Treasury")).Value,
                TreasurySmartContractAddressNameProvider.Name,
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList());
            return l;
        }
    }
}