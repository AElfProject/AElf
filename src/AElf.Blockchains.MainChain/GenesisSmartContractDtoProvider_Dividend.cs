using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Dividend;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForDividend(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Dividend")).Value,
                DividendSmartContractAddressNameProvider.Name,
                GenerateDividendInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateDividendInitializationCallList()
        {
            var dividendMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            dividendMethodCallList.Add(nameof(DividendContractContainer.DividendContractStub.InitializeDividendContract),
                new InitialDividendContractInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });
            return dividendMethodCallList;
        }
    }
}