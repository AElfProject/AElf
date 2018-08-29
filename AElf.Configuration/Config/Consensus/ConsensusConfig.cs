using AElf.Common.Enums;

namespace AElf.Configuration.Config.Consensus
{
    public class ConsensusConfig : ConfigBase<ConsensusConfig>
    {
        public ConsensusType ConsensusType { get; set; }

        public bool IsConsensusInfoGenerator { get; set; }

        public ConsensusConfig()
        {
            ConsensusType = ConsensusType.AElfDPoS;
        }
    }
}