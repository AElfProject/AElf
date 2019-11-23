using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionSizeFeeUnitForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly ITransactionSizeFeeUnitPriceProvider _transactionSizeFeeUnitPriceProvider;

        public TransactionSizeFeeUnitForkCacheHandler(ITransactionSizeFeeUnitPriceProvider transactionSizeFeeUnitPriceProvider)
        {
            _transactionSizeFeeUnitPriceProvider = transactionSizeFeeUnitPriceProvider;
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            _transactionSizeFeeUnitPriceProvider.RemoveForkCache(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            _transactionSizeFeeUnitPriceProvider.SetIrreversedCache(blockIndexes);
        }
    }
}