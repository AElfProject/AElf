using AElf.Kernel.Consensus.AEDPoS;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
{
    // TODO: Same code in src/AElf.Blockchains.MainChain/AEDPoSContractInitializationDataProvider, need to resolve.
    public class MainChainAEDPoSContractInitializationDataProvider : IAEDPoSContractInitializationDataProvider
    {
        private readonly AEDPoSOptions _aeDPoSOptions;

        public MainChainAEDPoSContractInitializationDataProvider(IOptionsSnapshot<AEDPoSOptions> aeDPoSOptions)
        {
            _aeDPoSOptions = aeDPoSOptions.Value;
        }

        public AEDPoSContractInitializationData GetContractInitializationData()
        {
            return new AEDPoSContractInitializationData
            {
                MiningInterval = _aeDPoSOptions.MiningInterval,
                PeriodSeconds = _aeDPoSOptions.PeriodSeconds,
                StartTimestamp = _aeDPoSOptions.StartTimestamp,
                InitialMinerList = _aeDPoSOptions.InitialMinerList,
                MinerIncreaseInterval = _aeDPoSOptions.MinerIncreaseInterval
            };
        }
    }
}