using System;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.EventMessages
{
    public class ConsensusRequestMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public long PreviousBlockHeight { get; }
        public TimeSpan TimeSpan { get; }
        public DateTime BlockTime { get; }

        public ConsensusRequestMiningEventData(Hash previousBlockHash, long previousBlockHeight, DateTime blockTime,
            TimeSpan timeSpan)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            BlockTime = blockTime;
            TimeSpan = timeSpan;
        }
    }
}