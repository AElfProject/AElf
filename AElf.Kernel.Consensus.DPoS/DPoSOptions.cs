using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> InitialMiners { get; set; }
        public int MiningInterval { get; set; }
        public bool IsBootMiner { get; set; }
        public string StartTimestamp { get; set; } = "2019-01-01 00:00:00";
        public long InitialTermNumber { get; set; }
        public bool Verbose { get; set; }
        public bool IsBlockchainAgeSettable { get; set; }
        public bool IsTimeSlotSkippable { get; set; }
    }
}