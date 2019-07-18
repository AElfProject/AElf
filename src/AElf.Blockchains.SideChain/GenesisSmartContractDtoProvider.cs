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
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes =
            ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>();
        
        private readonly ContractOptions _contractOptions;
        private readonly ConsensusOptions _consensusOptions;
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions, 
            IOptionsSnapshot<ContractOptions> contractOptions, ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _consensusOptions = consensusOptions.Value;
            _contractOptions = contractOptions.Value;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Consensus.AEDPoS")).Value,
                ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList(chainInitializationData));

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("MultiToken")).Value,
                TokenSmartContractAddressNameProvider.Name);

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("CrossChain")).Value,
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList(chainInitializationData));

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("ParliamentAuth")).Value,
                ParliamentAuthSmartContractAddressNameProvider.Name,
                GenerateParliamentInitializationCallList(chainInitializationData));
            
            return l;
        }
    }
}