using System;
using AElf.Common;

namespace AElf.Kernel
{
    public class BlockMiningEventData
    {
        public int ChainId { get; }
        public Hash PreviousBlockHash { get; }
        public ulong PreviousBlockHeight { get; }
        public DateTime DueTime { get; }

        public BlockMiningEventData(int chainId, Hash previousBlockHash, ulong previousBlockHeight, DateTime dueTime)
        {
            ChainId = chainId;
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            DueTime = dueTime;
        }
    }
}