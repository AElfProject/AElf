using System.Collections.Generic;
using AElf.Kernel;
using AElf.Node.Protocol;

// ReSharper disable once CheckNamespace
namespace AElf.Node
{
    public interface IBlockCollection
    {
        //List<PendingBlock> PendingBlocks { get; set; }
        List<Transaction> AddPendingBlock(PendingBlock pendingBlock);
        void RemovePendingBlock(PendingBlock pendingBlock);
        int Count { get; }
        int BranchedChainsCount { get; }
        ulong SyncedHeight { get; }
        List<PendingBlock> GetPendingBlocksFromBranchedChains();
    }
}