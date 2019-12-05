using System.Collections.Generic;
using System.Threading.Tasks;
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

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _transactionSizeFeeUnitPriceProvider.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _transactionSizeFeeUnitPriceProvider.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
    
    public class TransactionFeeCalculatorCoefficientForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly ICalculateStrategyProvider _calculateStrategyProvider;

        public TransactionFeeCalculatorCoefficientForkCacheHandler(ICalculateStrategyProvider calculateStrategyProvider)
        {
            _calculateStrategyProvider = calculateStrategyProvider;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _calculateStrategyProvider.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _calculateStrategyProvider.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}