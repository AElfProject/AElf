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

        public void RemoveForkCache(List<Hash> blockHashes)
        {
            _blockTransactionLimitProvider.RemoveForkCache(blockHashes);
        }

        public void SetIrreversedCache(List<Hash> blockHashes)
        {
            _blockTransactionLimitProvider.SetIrreversedCache(blockHashes);
        }

        public void SetIrreversedCache(Hash blockHash)
        {
            _blockTransactionLimitProvider.SetIrreversedCache(blockHash);
        }
    }
}