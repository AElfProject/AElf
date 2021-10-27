using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CrossChain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public class AEDPoSContractInitializationDataProvider : IAEDPoSContractInitializationDataProvider,
        ITransientDependency
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;
        private readonly ConsensusOptions _consensusOptions;

        public AEDPoSContractInitializationDataProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions,
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _consensusOptions = consensusOptions.Value;
        }

        public AEDPoSContractInitializationData GetContractInitializationData()
        {
            var sideChainInitializationData =
                AsyncHelper.RunSync(_sideChainInitializationDataProvider.GetChainInitializationDataAsync);

            var aedPoSContractInitializationData = new AEDPoSContractInitializationData
            {
                InitialMinerList = sideChainInitializationData == null
                    ? _consensusOptions.InitialMinerList
                    : MinerListWithRoundNumber.Parser
                        .ParseFrom(sideChainInitializationData.ChainInitializationConsensusInfo.InitialConsensusData)
                        .MinerList.Pubkeys.Select(p => p.ToHex()).ToList(),
                StartTimestamp = sideChainInitializationData?.CreationTimestamp ?? _consensusOptions.StartTimestamp,
                PeriodSeconds = _consensusOptions.PeriodSeconds,
                MiningInterval = _consensusOptions.MiningInterval,
                IsSideChain = true
            };

            return aedPoSContractInitializationData;
        }
    }
}