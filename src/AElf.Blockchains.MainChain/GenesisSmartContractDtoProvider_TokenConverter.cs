using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.TokenConverter;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTokenConverter()
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("TokenConverter")).Value,
                TokenConverterSmartContractAddressNameProvider.Name,
                GenerateTokenConverterInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenConverterInitializationCallList()
        {
            return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        }
    }
}