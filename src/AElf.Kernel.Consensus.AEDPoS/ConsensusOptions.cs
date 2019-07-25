using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public class ConsensusOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> InitialMiners { get; set; }
        public int MiningInterval { get; set; }
        public Timestamp StartTimestamp { get; set; } = new Timestamp {Seconds = 0};
        public long TimeEachTerm { get; set; } = 604800;

        public long MinerIncreaseInterval { get; set; } = 31536000;
    }
}