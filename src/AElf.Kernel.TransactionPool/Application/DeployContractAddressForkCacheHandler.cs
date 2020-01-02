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
        private readonly ICalculateNetCostStrategy _netCostStrategy;
        private readonly ICalculateStoCostStrategy _stoCostStrategy;
        private readonly ICalculateTxCostStrategy _txCostStrategy;

        public TransactionFeeCalculatorCoefficientForkCacheHandler(
            ICalculateReadCostStrategy readCostStrategy,
            ICalculateWriteCostStrategy writeCostStrategy,
            ICalculateStoCostStrategy stoCostStrategy,
            ICalculateNetCostStrategy netCostStrategy,
            ICalculateTxCostStrategy txCostStrategy)
        {
            _readCostStrategy = readCostStrategy;
            _writeCostStrategy = writeCostStrategy;
            _stoCostStrategy = stoCostStrategy;
            _netCostStrategy = netCostStrategy;
            _txCostStrategy = txCostStrategy;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _readCostStrategy.RemoveForkCache(blockIndexes);
            _writeCostStrategy.RemoveForkCache(blockIndexes);
            _stoCostStrategy.RemoveForkCache(blockIndexes);
            _netCostStrategy.RemoveForkCache(blockIndexes);
            _txCostStrategy.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _readCostStrategy.SetIrreversedCache(blockIndexes);
            _writeCostStrategy.SetIrreversedCache(blockIndexes);
            _stoCostStrategy.SetIrreversedCache(blockIndexes);
            _netCostStrategy.SetIrreversedCache(blockIndexes);
            _txCostStrategy.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}