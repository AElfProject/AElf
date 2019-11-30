using System.Collections.Generic;
using System.Threading.Tasks;
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
        
        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _blockTransactionLimitProvider.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _blockTransactionLimitProvider.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}