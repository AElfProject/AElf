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
        private readonly ICalculateFeeService _calculateFeeService;

        public TransactionFeeCalculatorCoefficientForkCacheHandler(ICalculateFeeService calculateFeeService)
        {
            _calculateFeeService = calculateFeeService;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _calculateFeeService.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _calculateFeeService.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}