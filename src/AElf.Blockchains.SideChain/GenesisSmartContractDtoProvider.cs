using System.Collections.Generic;
using System.Linq;
using AElf.Blockchains.BasicBaseChain;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.CrossChain;
using AElf.CrossChain.Grpc;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    // TODO: Split different contract and change this to plugin
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly ChainOptions _chainOptions;
        private readonly ContractOptions _contractOptions;
        private readonly ConsensusOptions _consensusOptions;
        private readonly CrossChainConfigOption _crossChainConfigOptions;
        private readonly IChainInitializationPlugin _chainInitializationPlugin;
        
        public GenesisSmartContractDtoProvider(IOptionsProvider optionsProvider, IChainInitializationPlugin chainInitializationPlugin)
        {
            _chainOptions = optionsProvider.ChainOptions;
            _consensusOptions = optionsProvider.ConsensusOptions;
            _crossChainConfigOptions = optionsProvider.CrossChainConfigOption;
            _contractOptions = optionsProvider.ContractOptions;
            _chainInitializationPlugin = chainInitializationPlugin;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            var sideChainInitializationResponse = AsyncHelper.RunSync(async () =>
                await _chainInitializationPlugin.RequestChainInitializationContextAsync(_chainOptions.ChainId));
            var chainInitializationContext = new ChainInitializationInformation
            {
                ChainId = sideChainInitializationResponse.ChainId,
                Creator = sideChainInitializationResponse.Creator,
                CreationTimestamp = sideChainInitializationResponse.CreationTimestamp,
                CreationHeightOnParentChain = sideChainInitializationResponse.CreationHeightOnParentChain
            };
            chainInitializationContext.ExtraInformation.AddRange(sideChainInitializationResponse.ExtraInformation);

            l.AddGenesisSmartContract<AEDPoSContract>(
                ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList(chainInitializationContext));

            l.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList());

            l.AddGenesisSmartContract<CrossChainContract>(
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList(chainInitializationContext));

            return l;
        }

        public ContractZeroInitializationInput GetContractZeroInitializationInput()
        {
            var contractZeroInitializationInput = new ContractZeroInitializationInput
            {
                ZeroOwnerAddressGenerationContractHashName = ParliamentAuthContractAddressNameProvider.Name,
                ZeroOwnerAddressGenerationMethodName = nameof(ParliamentAuthContract.GetZeroOwnerAddress),
                ContractDeploymentAuthorityRequired = _contractOptions.ContractDeploymentAuthorityRequired
            };
            
            return contractZeroInitializationInput;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenInitializationCallList()
        {
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            return tokenContractCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList(ChainInitializationInformation chainInitializationContext)
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            var miners = chainInitializationContext == null
                ? new MinerList
                {
                    PublicKeys =
                    {
                        _consensusOptions.InitialMiners.Select(p => p.ToByteString())
                    }
                }
                : MinerListWithRoundNumber.Parser.ParseFrom(chainInitializationContext.ExtraInformation[0]).MinerList;
            var timestamp = chainInitializationContext?.CreationTimestamp.ToDateTime() ?? _consensusOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(AEDPoSContract.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    IsSideChain = true
                });
            consensusMethodCallList.Add(nameof(AEDPoSContract.FirstRound),
                miners.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval, timestamp.ToUniversalTime()));
            return consensusMethodCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList(ChainInitializationInformation chainInitializationContext)
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContract.Initialize),
                new InitializeInput
                {
                    ParentChainId = _crossChainConfigOptions.ParentChainId,
                    CreationHeightOnParentChain = chainInitializationContext.CreationHeightOnParentChain
                });
            return crossChainMethodCallList;
        }
    }
}