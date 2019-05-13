using System.Collections.Generic;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.CrossChain.Grpc;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    // TODO: Split different contract and change this to plugin
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly ChainOptions _chainOptions;
        private readonly DPoSOptions _dposOptions;
        private readonly CrossChainConfigOption _crossChainConfigOptions;
        private readonly IChainInitializationPlugin _chainInitializationPlugin;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ChainOptions> chainOptions,
            IOptionsSnapshot<DPoSOptions> dposOptions, IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOptions,
            IChainInitializationPlugin chainInitializationPlugin)
        {
            _chainOptions = chainOptions.Value;
            _dposOptions = dposOptions.Value;
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

            l.AddGenesisSmartContract<ConsensusContract>(
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
//            var chainInitializationContextByteString = AsyncHelper.RunSync(async () =>
//                await _chainInitializationPlugin.RequestChainInitializationContextAsync(_chainOptions.ChainId)).ToByteString();
            //var chainInitializationContext = ChainInitializationContext.Parser.ParseFrom(chainInitializationContextByteString);
            var miners = chainInitializationContext == null
                ? _dposOptions.InitialMiners.ToMiners()
                : MinerListWithRoundNumber.Parser.ParseFrom(chainInitializationContext.ExtraInformation[0]).MinerList;
            var timestamp = chainInitializationContext?.CreationTimestamp.ToDateTime() ?? _dposOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                miners.GenerateFirstRoundOfNewTerm(_dposOptions.MiningInterval, timestamp.ToUniversalTime()));
            consensusMethodCallList.Add(nameof(ConsensusContract.ConfigStrategy),
                new DPoSStrategyInput
                {
                    IsBlockchainAgeSettable = _dposOptions.IsBlockchainAgeSettable,
                    IsTimeSlotSkippable = _dposOptions.IsTimeSlotSkippable,
                    IsVerbose = _dposOptions.Verbose
                });
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