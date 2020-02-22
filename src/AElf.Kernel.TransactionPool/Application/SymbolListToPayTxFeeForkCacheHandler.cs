using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    //TODO: not here
    public class SymbolListToPayTxFeeForkCacheHandler : IForkCacheHandler, ITransientDependency
    {
        private readonly ISymbolListToPayTxFeeService _symbolListToPayTxFeeService;

        public SymbolListToPayTxFeeForkCacheHandler(ISymbolListToPayTxFeeService symbolListToPayTxFeeService)
        {
            _symbolListToPayTxFeeService = symbolListToPayTxFeeService;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _symbolListToPayTxFeeService.RemoveFromForkCacheByBlockIndex(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _symbolListToPayTxFeeService.SyncCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}