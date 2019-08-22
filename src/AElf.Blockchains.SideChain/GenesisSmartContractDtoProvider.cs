using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Deployer;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes;
        
        private readonly ContractOptions _contractOptions;
        private readonly ConsensusOptions _consensusOptions;
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public ILogger<GenesisSmartContractDtoProvider> Logger { get; set; }

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions, 
            IOptionsSnapshot<ContractOptions> contractOptions, ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _consensusOptions = consensusOptions.Value;
            _contractOptions = contractOptions.Value;
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(_contractOptions
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            var genesisSmartContractDtoList = new List<GenesisSmartContractDto>();

            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());

            if (chainInitializationData == null)
            {
                Logger.LogWarning("Chain initialization data is null.");
                return genesisSmartContractDtoList;
            }
            
            // chainInitializationData cannot be null if it is first time side chain startup. 
            genesisSmartContractDtoList.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Consensus.AEDPoS")).Value,
                ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList(chainInitializationData));

            genesisSmartContractDtoList.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("MultiToken")).Value,
                TokenSmartContractAddressNameProvider.Name, GenerateTokenInitializationCallList(chainInitializationData));

            genesisSmartContractDtoList.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("CrossChain")).Value,
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList(chainInitializationData));

            genesisSmartContractDtoList.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("ParliamentAuth")).Value,
                ParliamentAuthSmartContractAddressNameProvider.Name,
                GenerateParliamentInitializationCallList(chainInitializationData));

            return genesisSmartContractDtoList;
        }
    }
}