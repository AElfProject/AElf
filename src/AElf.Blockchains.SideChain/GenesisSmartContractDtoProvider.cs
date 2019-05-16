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
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
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
        
        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ChainOptions> chainOptions,
            IOptionsSnapshot<ConsensusOptions> consensusOptions, IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOptions,
            IOptionsSnapshot<ContractOptions> contractOptions, IChainInitializationPlugin chainInitializationPlugin)
        {
            _chainOptions = chainOptions.Value;
            _consensusOptions = consensusOptions.Value;
            _crossChainConfigOptions = crossChainConfigOptions.Value;
            _contractOptions = contractOptions.Value;
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
            var contractZeroInitializationInput = new ContractZeroInitializationInput();
            if (!_contractOptions.IsContractDeploymentAllowed)
            {
                contractZeroInitializationInput.ParliamentAuthContractName =
                    ParliamentAuthContractAddressNameProvider.Name;
            }
            
            return contractZeroInitializationInput;
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
                ? new Miners
                {
                    PublicKeys =
                    {
                        _consensusOptions.InitialMiners.Select(p =>
                            ByteString.CopyFrom(ByteArrayHelpers.FromHexString(p)))
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