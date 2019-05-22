using System;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;

namespace AElf.Kernel.Consensus
{
    public class ConsensusRequestMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public long PreviousBlockHeight { get; }
        public TimeSpan BlockExecutionTime { get; }
        public Timestamp BlockTime { get; }

        public ConsensusRequestMiningEventData(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
            TimeSpan blockExecutionTime)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            BlockTime = blockTime;
            BlockExecutionTime = blockExecutionTime;
        }
    }
}