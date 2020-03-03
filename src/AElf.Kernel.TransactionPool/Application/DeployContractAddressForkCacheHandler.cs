using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    //TODO: should not implement here, no fork

    public class TransactionFeeCalculatorCoefficientForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        public TransactionFeeCalculatorCoefficientForkCacheHandler()
        {
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            return Task.CompletedTask;
        }
    }
}