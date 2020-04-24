using AElf.Kernel.Consensus.AEDPoS;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
{
    // TODO: Same code in src/AElf.Blockchains.MainChain/AEDPoSContractInitializationDataProvider, need to resolve.
    public class MainChainAEDPoSContractInitializationDataProvider : IAEDPoSContractInitializationDataProvider
    {
        private readonly ConsensusOptions _consensusOptions;

        public MainChainAEDPoSContractInitializationDataProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions)
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