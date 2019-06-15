using System;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;

namespace AElf.Kernel.Consensus
{
    public class ConsensusRequestMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public long PreviousBlockHeight { get; }
        public Duration BlockExecutionTime { get; }
        public Timestamp BlockTime { get; }
        
        public DateTime CreateTime { get; set; }

        public ConsensusRequestMiningEventData(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
            Duration blockExecutionTime)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            BlockTime = blockTime;
            BlockExecutionTime = blockExecutionTime;
            CreateTime = DateTime.Now;
        }
    }
}