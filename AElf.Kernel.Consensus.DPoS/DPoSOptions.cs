using System.Collections.Generic;

namespace AElf.Kernel.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<string> InitialMiners { get; set; }
        public int MiningInterval { get; set; }
        public bool IsFirstMiner { get; set; }
    }
}