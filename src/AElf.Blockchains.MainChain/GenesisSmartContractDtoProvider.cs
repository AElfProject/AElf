using System.Collections.Generic;
using System.Linq;
using AElf.Blockchains.BasicBaseChain;
using AElf.Contracts.Deployer;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.OS;
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
        private readonly IReadOnlyDictionary<string, byte[]> _codes;
        
        private readonly ConsensusOptions _consensusOptions;
        private readonly EconomicOptions _economicOptions;
        private readonly ContractOptions _contractOptions;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ConsensusOptions> dposOptions,
            IOptionsSnapshot<EconomicOptions> economicOptions, IOptionsSnapshot<ContractOptions> contractOptions)
        {
            _consensusOptions = dposOptions.Value;
            _economicOptions = economicOptions.Value;
            _contractOptions = contractOptions.Value;
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(_contractOptions
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            // The order matters !!!
            return new[]
            {
                GetGenesisSmartContractDtosForVote(zeroContractAddress),
                GetGenesisSmartContractDtosForProfit(zeroContractAddress),
                GetGenesisSmartContractDtosForElection(zeroContractAddress),
                GetGenesisSmartContractDtosForTreasury(),
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