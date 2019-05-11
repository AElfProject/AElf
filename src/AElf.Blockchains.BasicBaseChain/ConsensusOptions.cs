using System;
using System.Collections.Generic;

namespace AElf.Blockchains
{
    // ReSharper disable once InconsistentNaming
    public class ConsensusOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> InitialMiners { get; set; }
        public int MiningInterval { get; set; }
        public DateTime StartTimestamp { get; set; } = DateTime.MinValue;
        public int TimeEachTerm { get; set; } = 7;
    }
}