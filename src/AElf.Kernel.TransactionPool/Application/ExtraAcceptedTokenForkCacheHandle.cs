using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class ExtraAcceptedTokenForkCacheHandle : IForkCacheHandler, ITransientDependency
    {
        private readonly IExtraAcceptedTokenService _extraAcceptedTokenService;

        public ExtraAcceptedTokenForkCacheHandle(IExtraAcceptedTokenService extraAcceptedTokenService)
        {
            _extraAcceptedTokenService = extraAcceptedTokenService;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _extraAcceptedTokenService.RemoveFromForkCacheByBlockIndex(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _extraAcceptedTokenService.SyncCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}