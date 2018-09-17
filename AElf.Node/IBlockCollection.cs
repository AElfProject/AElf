using System.Collections.Generic;
using AElf.Node.Protocol;

// ReSharper disable once CheckNamespace
namespace AElf.Node
{
    public interface IBlockCollection
    {
        List<PendingBlock> PendingBlocks { get; set; }
        List<PendingBlock> AddPendingBlock(PendingBlock pendingBlock);
        void RemovePendingBlock(PendingBlock pendingBlock);
        int Count { get; }
        int BranchedChainsCount { get; }
        ulong PendingBlockHeight { get; set; }
        ulong SyncedHeight { get; }
        List<PendingBlock> GetPendingBlocksFromBranchedChains();
    }
}