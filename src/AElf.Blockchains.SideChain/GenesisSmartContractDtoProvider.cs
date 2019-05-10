using System.Collections.Generic;
using System.Linq;
using Acs0;
using Acs4;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Deployer;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;
using ChainInitializationContext = AElf.Contracts.CrossChain.ChainInitializationContext;

namespace AElf.Blockchains.SideChain
{
    // TODO: Split different contract and change this to plugin
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes =
            ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>();
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

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Consensus.DPoS")).Value,
                ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList());

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("MultiToken")).Value,
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList());

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("CrossChain")).Value,
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenInitializationCallList()
        {
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            return tokenContractCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList()
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var chainInitializationContextByteString = AsyncHelper.RunSync(async () =>
                await _chainInitializationPlugin.RequestChainInitializationContextAsync(_chainOptions.ChainId)).ToByteString();
            var chainInitializationContext = ChainInitializationContext.Parser.ParseFrom(chainInitializationContextByteString);
            var miners = chainInitializationContext == null
                ? _dposOptions.InitialMiners.ToMiners()
                : MinerListWithRoundNumber.Parser.ParseFrom(chainInitializationContext.ExtraInformation[0]).MinerList;
            var timestamp = chainInitializationContext?.CreatedTime.ToDateTime() ?? _dposOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(ConsensusContractContainer.ConsensusContractStub.InitialConsensus),
                miners.GenerateFirstRoundOfNewTerm(_dposOptions.MiningInterval, timestamp.ToUniversalTime()));
            consensusMethodCallList.Add(nameof(ConsensusContractContainer.ConsensusContractStub.ConfigStrategy),
                new DPoSStrategyInput
                {
                    IsBlockchainAgeSettable = _dposOptions.IsBlockchainAgeSettable,
                    IsTimeSlotSkippable = _dposOptions.IsTimeSlotSkippable,
                    IsVerbose = _dposOptions.Verbose
                });
            return consensusMethodCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList()
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new AElf.Contracts.CrossChain.InitializeInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    ParentChainId = _crossChainConfigOptions.ParentChainId,
                    ParliamentContractSystemName = ParliamentAuthContractAddressNameProvider.Name
                });
            return crossChainMethodCallList;
        }
    }
}