using AElf.Common.Enums;

namespace AElf.Configuration.Config.Consensus
{
    public class ConsensusConfig : ConfigBase<ConsensusConfig>
    {
        public ConsensusType ConsensusType { get; set; }

        public int DPoSMiningInterval { get; set; }

        public ulong ExpectedTransanctionCount { get; set; }

        public int SingleNodeTestMiningInterval { get; set; }

        public ConsensusConfig()
        {
            ConsensusType = ConsensusType.AElfDPoS;
            DPoSMiningInterval = 4000;
            ExpectedTransanctionCount = 8000;
            SingleNodeTestMiningInterval = 4000;
        }
    }
}