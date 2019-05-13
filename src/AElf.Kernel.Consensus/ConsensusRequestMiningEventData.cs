using System;

namespace AElf.Kernel.Consensus
{
    public class ConsensusRequestMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public long PreviousBlockHeight { get; }
        public TimeSpan BlockExecutionTime { get; }
        public DateTime BlockTime { get; }

        public ConsensusRequestMiningEventData(Hash previousBlockHash, long previousBlockHeight, DateTime blockTime,
            TimeSpan blockExecutionTime)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            BlockTime = blockTime;
            BlockExecutionTime = blockExecutionTime;
        }
    }
}