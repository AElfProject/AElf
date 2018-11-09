using AElf.Common.Enums;

namespace AElf.Configuration.Config.Consensus
{
    // ReSharper disable InconsistentNaming
    [ConfigFile(FileName = "consensus.json")]
    public class ConsensusConfig : ConfigBase<ConsensusConfig>
    {
        public ConsensusType ConsensusType { get; set; }

        public int DPoSMiningInterval { get; set; }

        public ulong ExpectedTransactionCount { get; set; }

        public int SingleNodeTestMiningInterval { get; set; }
    }
}