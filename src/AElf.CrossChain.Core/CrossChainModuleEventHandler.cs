using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Events;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    internal class CrossChainModuleEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ILocalEventHandler<InitialSyncFinishedEvent>, ITransientDependency
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;

        public IOptionsMonitor<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }
            
        public CrossChainModuleEventHandler(ICrossChainDataProvider crossChainDataProvider, 
            ICrossChainCacheEntityService crossChainCacheEntityService, 
            IIrreversibleBlockStateProvider irreversibleBlockStateProvider)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _crossChainCacheEntityService = crossChainCacheEntityService;
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
        }
        
        public async Task HandleEventAsync(InitialSyncFinishedEvent eventData)
        {
            CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored = false;
            var isReadyToCreateChainCache = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            if (!isReadyToCreateChainCache)
                return;
            var libIdHeight = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            _ = _crossChainCacheEntityService.RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            if (!CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored)
            {
                await _crossChainCacheEntityService.RegisterNewChainsAsync(eventData.BlockHash, eventData.BlockHeight);
            }
            
            _crossChainDataProvider.UpdateWithLibIndex(new BlockIndex
            {
                Hash = eventData.BlockHash,
                Height = eventData.BlockHeight
            });
        }
    }
}