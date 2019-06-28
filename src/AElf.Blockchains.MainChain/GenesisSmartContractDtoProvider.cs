using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Deployer;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes =
            ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>();
        
        private readonly ConsensusOptions _consensusOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;
        private readonly ContractOptions _contractOptions;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ConsensusOptions> dposOptions,
            IOptionsSnapshot<TokenInitialOptions> tokenInitialOptions, IOptionsSnapshot<ContractOptions> contractOptions)
        {
            _consensusOptions = dposOptions.Value;
            _tokenInitialOptions = tokenInitialOptions.Value;
            _contractOptions = contractOptions.Value;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            // The order matters !!!
            return new[]
            {
                GetGenesisSmartContractDtosForVote(zeroContractAddress),
                GetGenesisSmartContractDtosForProfit(zeroContractAddress),
                GetGenesisSmartContractDtosForElection(zeroContractAddress),
                GetGenesisSmartContractDtosForToken(zeroContractAddress),
//                GetGenesisSmartContractDtosForResource(zeroContractAddress),
                GetGenesisSmartContractDtosForCrossChain(zeroContractAddress),
                GetGenesisSmartContractDtosForParliament(),
                GetGenesisSmartContractDtosForConsensus(zeroContractAddress),
                GetGenesisSmartContractDtosForConfiguration(zeroContractAddress)
            }.SelectMany(x => x);
        }
    }
}