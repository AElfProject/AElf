using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Deployer;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    /// <summary>
    /// Provide dtos for genesis block contract deployment and initialization.
    /// </summary>
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes =
            ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>();
        private readonly ConsensusOptions _consensusOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ConsensusOptions> dposOptions,
            IOptionsSnapshot<TokenInitialOptions> tokenInitialOptions)
        {
            _consensusOptions = dposOptions.Value;
            _tokenInitialOptions = tokenInitialOptions.Value;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            // The order matters !!!
            return new[]
            {
                GetGenesisSmartContractDtosForVote(zeroContractAddress),
                GetGenesisSmartContractDtosForProfit(zeroContractAddress),
                GetGenesisSmartContractDtosForTreasury(),
                GetGenesisSmartContractDtosForElection(zeroContractAddress),
                GetGenesisSmartContractDtosForToken(zeroContractAddress),
                GetGenesisSmartContractDtosForCrossChain(zeroContractAddress),
                GetGenesisSmartContractDtosForParliament(),
                GetGenesisSmartContractDtosForConfiguration(zeroContractAddress),
                GetGenesisSmartContractDtosForConsensus(),
                GetGenesisSmartContractDtosForTokenConverter(),
                // Economic Contract should always be the last one to deploy and initialize.
                GetGenesisSmartContractDtosForEconomic()
            }.SelectMany(x => x);
        }
    }
}