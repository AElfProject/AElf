using System.Collections.Generic;

namespace AElf.Consensus
{
    public class ConsensusOptions
    {
        public List<string> InitialMiners { get; set; }
        public int MiningInterval { get; set; }
    }
}