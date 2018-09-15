using System.Collections.Generic;
using AElf.Kernel;
using AElf.Node.Protocol;
using NLog;

namespace AElf.Node
{
    public class SyncService : ISyncService
    {
        private readonly IBlockCollection _blockCollection;

        private readonly ILogger _logger;

        public SyncService(ILogger logger)
        {
            _logger = logger;
            _blockCollection = new BlockCollection(_logger);
        }
        
        public List<PendingBlock> PendingBlocks
        {
            get => _blockCollection.PendingBlocks;
            set => _blockCollection.PendingBlocks = value;
        }
        
        public void AddPendingBlock(PendingBlock pendingBlock)
        {
            _blockCollection.AddPendingBlock(pendingBlock);
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