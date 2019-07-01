using System.Collections.Generic;
using System.Linq;
using Acs0;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Deployer;
using AElf.CrossChain;
using AElf.CrossChain.Communication;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    // TODO: Split different contract and change this to plugin
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes =
            ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>();
        
        private readonly ChainOptions _chainOptions;
        private readonly ConsensusOptions _consensusOptions;
        private readonly CrossChainConfigOptions _crossChainConfigOptions;
        private readonly IChainInitializationDataPlugin _chainInitializationDataPlugin;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ChainOptions> chainOptions,
            IOptionsSnapshot<ConsensusOptions> consensusOptions, IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOptions,
            IChainInitializationDataPlugin chainInitializationDataPlugin)
        {
            _chainOptions = chainOptions.Value;
            _consensusOptions = consensusOptions.Value;
            _crossChainConfigOptions = crossChainConfigOptions.Value;
            _chainInitializationDataPlugin = chainInitializationDataPlugin;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _chainInitializationDataPlugin.GetChainInitializationDataAsync(_chainOptions.ChainId));

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Consensus.AEDPoS")).Value,
                ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList(chainInitializationData));

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("MultiToken")).Value,
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList());

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("CrossChain")).Value,
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList(chainInitializationData));

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenInitializationCallList()
        {
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            return tokenContractCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            var miners = chainInitializationData == null
                ? new MinerList
                {
                    Pubkeys =
                    {
                        _consensusOptions.InitialMiners.Select(p => p.ToByteString())
                    }
                }
                : MinerListWithRoundNumber.Parser.ParseFrom(chainInitializationData.ExtraInformation[0]).MinerList;
            var timestamp = chainInitializationData?.CreationTimestamp ?? _consensusOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    IsSideChain = true
                });
            consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                miners.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval, timestamp));
            return consensusMethodCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new AElf.Contracts.CrossChain.InitializeInput
                {
                    ParentChainId = _crossChainConfigOptions.ParentChainId,
                    CreationHeightOnParentChain = chainInitializationData.CreationHeightOnParentChain
                });
            return crossChainMethodCallList;
        }
    }
}