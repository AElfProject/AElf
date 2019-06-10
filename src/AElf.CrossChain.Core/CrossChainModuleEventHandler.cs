using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    internal class CrossChainModuleEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ISingletonDependency
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;

        public CrossChainModuleEventHandler(ICrossChainDataProvider crossChainDataProvider, ICrossChainCacheEntityService crossChainCacheEntityService)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _crossChainCacheEntityService = crossChainCacheEntityService;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _crossChainCacheEntityService.RegisterNewChainsAsync(eventData.BlockHash, eventData.BlockHeight);
            _crossChainDataProvider.UpdateWithLibIndex(new BlockIndex
            {
                Hash = eventData.BlockHash,
                Height = eventData.BlockHeight
            });
        }
    }
}