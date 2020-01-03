using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionFeeCalculatorCoefficientForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly ICalculateReadCostStrategy _readCostStrategy;
        private readonly ICalculateWriteCostStrategy _writeCostStrategy;
        private readonly ICalculateTrafficCostStrategy _trafficCostStrategy;
        private readonly ICalculateStorageCostStrategy _storageCostStrategy;
        private readonly ICalculateTxCostStrategy _txCostStrategy;

        public TransactionFeeCalculatorCoefficientForkCacheHandler(
            ICalculateReadCostStrategy readCostStrategy,
            ICalculateWriteCostStrategy writeCostStrategy,
            ICalculateStorageCostStrategy storageCostStrategy,
            ICalculateTrafficCostStrategy trafficCostStrategy,
            ICalculateTxCostStrategy txCostStrategy)
        {
            _readCostStrategy = readCostStrategy;
            _writeCostStrategy = writeCostStrategy;
            _storageCostStrategy = storageCostStrategy;
            _trafficCostStrategy = trafficCostStrategy;
            _txCostStrategy = txCostStrategy;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _readCostStrategy.RemoveForkCache(blockIndexes);
            _writeCostStrategy.RemoveForkCache(blockIndexes);
            _storageCostStrategy.RemoveForkCache(blockIndexes);
            _trafficCostStrategy.RemoveForkCache(blockIndexes);
            _txCostStrategy.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _readCostStrategy.SetIrreversedCache(blockIndexes);
            _writeCostStrategy.SetIrreversedCache(blockIndexes);
            _storageCostStrategy.SetIrreversedCache(blockIndexes);
            _trafficCostStrategy.SetIrreversedCache(blockIndexes);
            _txCostStrategy.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}