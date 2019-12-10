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
        private readonly ICalculateCpuCostStrategy _cpuCostStrategy;
        private readonly ICalculateRamCostStrategy _ramCostStrategy;
        private readonly ICalculateNetCostStrategy _netCostStrategy;
        private readonly ICalculateStoCostStrategy _stoCostStrategy;
        private readonly ICalculateTxCostStrategy _txCostStrategy;

        public TransactionFeeCalculatorCoefficientForkCacheHandler(
            ICalculateCpuCostStrategy cpuCostStrategy,
            ICalculateRamCostStrategy ramCostStrategy,
            ICalculateStoCostStrategy stoCostStrategy,
            ICalculateNetCostStrategy netCostStrategy,
            ICalculateTxCostStrategy txCostStrategy)
        {
            _cpuCostStrategy = cpuCostStrategy;
            _ramCostStrategy = ramCostStrategy;
            _stoCostStrategy = stoCostStrategy;
            _netCostStrategy = netCostStrategy;
            _txCostStrategy = txCostStrategy;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _cpuCostStrategy.RemoveForkCache(blockIndexes);
            _ramCostStrategy.RemoveForkCache(blockIndexes);
            _stoCostStrategy.RemoveForkCache(blockIndexes);
            _netCostStrategy.RemoveForkCache(blockIndexes);
            _txCostStrategy.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _cpuCostStrategy.SetIrreversedCache(blockIndexes);
            _ramCostStrategy.SetIrreversedCache(blockIndexes);
            _stoCostStrategy.SetIrreversedCache(blockIndexes);
            _netCostStrategy.SetIrreversedCache(blockIndexes);
            _txCostStrategy.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}