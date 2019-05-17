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
        public Timestamp StartTimestamp { get; set; } = DateTime.MinValue.ToUniversalTime().ToTimestamp();
        public long InitialTermNumber { get; set; }
        public int TimeEachTerm { get; set; }
    }
}