using System;
using System.Collections.Generic;

namespace AElf.Kernel.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> InitialMiners { get; set; }
        public int MiningInterval { get; set; }
        public bool IsBootMiner { get; set; }
        public DateTime StartTimestamp { get; set; } = DateTime.MinValue;
        public long InitialTermNumber { get; set; }
        public bool Verbose { get; set; }
        public bool IsBlockchainAgeSettable { get; set; }
        public bool IsTimeSlotSkippable { get; set; }
    }
}