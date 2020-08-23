using AElf.Kernel.Consensus.AEDPoS;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.MainChain
{
    public class AEDPoSContractInitializationDataProvider : IAEDPoSContractInitializationDataProvider,
        ITransientDependency
    {
        private readonly AEDPoSOptions _aeDPoSOptions;

        public AEDPoSContractInitializationDataProvider(IOptionsSnapshot<AEDPoSOptions> aeDPoSOptions)
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