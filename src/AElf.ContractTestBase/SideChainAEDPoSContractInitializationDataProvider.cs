using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CrossChain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.ContractTestBase
{
    public class SideChainAEDPoSContractInitializationDataProvider : IAEDPoSContractInitializationDataProvider
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;
        private readonly AEDPoSOptions _aeDPoSOptions;

        public SideChainAEDPoSContractInitializationDataProvider(IOptionsSnapshot<AEDPoSOptions> aeDPoSOptions,
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _aeDPoSOptions = aeDPoSOptions.Value;
        }

        public AEDPoSContractInitializationData GetContractInitializationData()
        {
            var sideChainInitializationData =
                AsyncHelper.RunSync(_sideChainInitializationDataProvider.GetChainInitializationDataAsync);

            var aedPoSContractInitializationData = new AEDPoSContractInitializationData
            {
                InitialMinerList = sideChainInitializationData == null
                    ? _aeDPoSOptions.InitialMinerList
                    : MinerListWithRoundNumber.Parser
                        .ParseFrom(sideChainInitializationData.ChainInitializationConsensusInfo.InitialMinerListData)
                        .MinerList.Pubkeys.Select(p => p.ToHex()).ToList(),
                StartTimestamp = sideChainInitializationData?.CreationTimestamp ?? _aeDPoSOptions.StartTimestamp,
                PeriodSeconds = _aeDPoSOptions.PeriodSeconds,
                MiningInterval = _aeDPoSOptions.MiningInterval,
                IsSideChain = true
            };

            return aedPoSContractInitializationData;
        }
    }
}