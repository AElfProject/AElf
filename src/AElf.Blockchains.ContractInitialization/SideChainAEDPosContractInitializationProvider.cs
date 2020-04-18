using System.Linq;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.ContractInitialization
{
    public class SideChainAEDPosContractInitializationProvider : ContractInitializationProviderBase
    {
        private readonly ConsensusOptions _consensusOptions;
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;
        
        protected override Hash ContractName { get; } = ConsensusSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Consensus.AEDPoS";

        public SideChainAEDPosContractInitializationProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions, ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _consensusOptions = consensusOptions.Value;
        }

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());
        
            var miners = chainInitializationData == null
                ? new MinerList
                {
                    Pubkeys =
                    {
                        _consensusOptions.InitialMinerList.Select(p => p.ToByteString())
                    }
                }
                : MinerListWithRoundNumber.Parser
                    .ParseFrom(chainInitializationData.ChainInitializationConsensusInfo.InitialMinerListData).MinerList;
            var timestamp = chainInitializationData?.CreationTimestamp ?? _consensusOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    IsSideChain = true,
                    PeriodSeconds = _consensusOptions.PeriodSeconds
                });
            consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                miners.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval, timestamp));
            return consensusMethodCallList;
        }
    }
}