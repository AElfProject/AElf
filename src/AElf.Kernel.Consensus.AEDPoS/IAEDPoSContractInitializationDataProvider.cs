using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS
{
    public interface IAEDPoSContractInitializationDataProvider
    {
        AEDPoSContractInitializationData GetContractInitializationData();
    }

    public class AEDPoSContractInitializationData
    {
        public long PeriodSeconds { get; set; }
        public long MinerIncreaseInterval { get; set; }
        public List<string> InitialMinerList { get; set; }
        public int MiningInterval { get; set; }
        public Timestamp StartTimestamp { get; set; }
        public bool IsSideChain { get; set; }
    }
}