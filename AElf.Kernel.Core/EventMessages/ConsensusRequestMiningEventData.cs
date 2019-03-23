using System;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.EventMessages
{
    public class ConsensusRequestMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public long PreviousBlockHeight { get; }
        public DateTime DueTime => DateTime.UtcNow.AddMilliseconds(TimeoutMilliseconds);
        public int TimeoutMilliseconds { get; }

        public ConsensusRequestMiningEventData(Hash previousBlockHash, long previousBlockHeight,
            int timeoutMilliseconds)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            TimeoutMilliseconds = timeoutMilliseconds;
        }
    }
}