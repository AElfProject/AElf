using System;
using AElf.Common;

namespace AElf.Kernel.EventMessages
{
    public class BlockMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public ulong PreviousBlockHeight { get; }
        public DateTime DueTime => DateTime.UtcNow.AddMilliseconds(TimeoutMilliseconds);
        public int TimeoutMilliseconds { get; }

        public BlockMiningEventData(Hash previousBlockHash, ulong previousBlockHeight,
            int timeoutMilliseconds)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            TimeoutMilliseconds = timeoutMilliseconds;
        }
    }
}