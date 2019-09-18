using System;
using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class RoundDto
    {
        public long RoundNumber { get; set; }
        public long TermNumber { get; set; }
        public long RoundId { get; set; }
        public Dictionary<string, MinerInRoundDto> RealTimeMinerInformation { get; set; }
        public string ExtraBlockProducerOfPreviousRound { get; set; }
        public long ConfirmedIrreversibleBlockRoundNumber { get; set; }
        public long ConfirmedIrreversibleBlockHeight { get; set; }
        public bool IsMinerListJustChanged { get; set; }
    }

    public class MinerInRoundDto
    {
        public int Order { get; set; }
        public int ProducedTinyBlocks { get; set; }
        public DateTime ExpectedMiningTime { get; set; }
        public List<DateTime> ActualMiningTimes { get; set; }
        public string InValue { get; set; }
        public string PreviousInValue { get; set; }
        public string OutValue { get; set; }
        public long ProducedBlocks { get; set; }
        public long MissedBlocks { get; set; }
        public long ImpliedIrreversibleBlockHeight { get; set; }
    }
}