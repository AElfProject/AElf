using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner.Application
{
    public class BlockTransactionLimitForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;

        public BlockTransactionLimitForkCacheHandler(IBlockTransactionLimitProvider blockTransactionLimitProvider)
        {
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
        }
        
        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            _blockTransactionLimitProvider.RemoveForkCache(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            _blockTransactionLimitProvider.SetIrreversedCache(blockIndexes);
        }
    }
}