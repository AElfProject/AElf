using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class RoundDto
    {
        public long RoundNumber { get; set; }
        public long TermNumber { get; set; }
        public long RoundId { get; set; }
        public Dictionary<string, MinerInRoundDto> RealTimeMinerInformation { get; set; }
        public string ExtraBlockProducerOfPreviousRound { get; set; }
    }

    public class MinerInRoundDto
    {
        public int Order { get; set; }
        public int ProducedTinyBlocks { get; set; }
        public Timestamp ExpectedMiningTime { get; set; }
        public List<Timestamp> ActualMiningTimes { get; set; }
        public Hash InValue { get; set; }
        public Hash PreviousInValue { get; set; }
        public Hash OutValue { get; set; }
        public long ProducedBlocks { get; set; }
        public long MissedBlocks { get; set; }
    }
}