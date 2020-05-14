using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS
{
    public class AEDPoSContractInitializationDataProvider : IAEDPoSContractInitializationDataProvider,
        ITransientDependency
    {
        private readonly ConsensusOptions _consensusOptions;

        public AEDPoSContractInitializationDataProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
        }

        public AEDPoSContractInitializationData GetContractInitializationData()
        {
            return new AEDPoSContractInitializationData
            {
                MiningInterval = _consensusOptions.MiningInterval,
                PeriodSeconds = _consensusOptions.PeriodSeconds,
                StartTimestamp = _consensusOptions.StartTimestamp,
                InitialMinerList = _consensusOptions.InitialMinerList,
                MinerIncreaseInterval = _consensusOptions.MinerIncreaseInterval
            };
        }
    }
}