using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForProfit()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.Profit"),
                ProfitSmartContractAddressNameProvider.Name,
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList());
            return l;
        }
    }
}