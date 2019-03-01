using System;
using AElf.Common;

namespace AElf.Kernel.EventMessages
{
    public class BlockMiningEventData
    {
        public int ChainId { get; }
        public Hash PreviousBlockHash { get; }
        public ulong PreviousBlockHeight { get; }
        public DateTime DueTime => DateTime.UtcNow.AddMilliseconds(TimeoutMilliseconds);
        public int TimeoutMilliseconds { get; }

        public BlockMiningEventData(int chainId, Hash previousBlockHash, ulong previousBlockHeight,
            int timeoutMilliseconds)
        {
            ChainId = chainId;
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            TimeoutMilliseconds = timeoutMilliseconds;
        }
    }
}