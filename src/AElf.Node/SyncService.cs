using System.Collections.Generic;
using AElf.ChainController;
using AElf.Kernel;
using AElf.Node.Protocol;
using NLog;

namespace AElf.Node
{
    public class SyncService : ISyncService
    {
        private readonly IBlockCollection _blockCollection;

        public SyncService(IChainService chainService, ILogger logger)
        {
            _blockCollection = new BlockCollection(chainService, logger);
        }
        
        public List<PendingBlock> PendingBlocks
        {
            get => BlockCollection.PendingBlocks;
            set => BlockCollection.PendingBlocks = value;
        }
        
        public List<Transaction> AddPendingBlock(PendingBlock pendingBlock)
        {
            return _blockCollection.AddPendingBlock(pendingBlock);
        }
        
        public void RemovePendingBlock(PendingBlock pendingBlock)
        {
            _blockCollection.RemovePendingBlock(pendingBlock);
        }

        public List<PendingBlock> GetPendingBlocksFromBranchedChains()
        {
            return _blockCollection.GetPendingBlocksFromBranchedChains();
        }

        public bool IsForked()
        {
            return _blockCollection.BranchedChainsCount > 0;
        }
    }
}