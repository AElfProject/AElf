using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.CrossChain.Grpc;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    // TODO: Split different contract and change this to plugin
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly ChainOptions _chainOptions;
        private readonly ConsensusOptions _consensusOptions;
        private readonly CrossChainConfigOption _crossChainConfigOptions;
        private readonly IChainInitializationPlugin _chainInitializationPlugin;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ChainOptions> chainOptions,
            IOptionsSnapshot<ConsensusOptions> consensusOptions, IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOptions,
            IChainInitializationPlugin chainInitializationPlugin)
        {
            _chainOptions = chainOptions.Value;
            _consensusOptions = consensusOptions.Value;
            _crossChainConfigOptions = crossChainConfigOptions.Value;
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

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenInitializationCallList()
        {
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
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
                        _consensusOptions.InitialMiners.Select(p => p.ToMappingKey())
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
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    ParentChainId = _crossChainConfigOptions.ParentChainId,
                    ParliamentContractSystemName = ParliamentAuthContractAddressNameProvider.Name,
                    CreationHeightOnParentChain = chainInitializationContext.CreationHeightOnParentChain
                });
            return crossChainMethodCallList;
        }
    }
}