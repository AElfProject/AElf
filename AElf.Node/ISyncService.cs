using System.Collections.Generic;
using AElf.Kernel;
using AElf.Node.Protocol;

namespace AElf.Node
{
    public interface ISyncService
    {
        List<PendingBlock> PendingBlocks { get; set; }
        List<Transaction> AddPendingBlock(PendingBlock pendingBlock);
        void RemovePendingBlock(PendingBlock pendingBlock);
        List<PendingBlock> GetPendingBlocksFromBranchedChains();
        bool IsForked();
    }
}