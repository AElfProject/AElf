using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    internal class CrossChainModuleEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ILocalEventHandler<InitialSyncFinishedEvent>, ITransientDependency
    {
        private readonly ICrossChainService _crossChainService;
            
        public CrossChainModuleEventHandler(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
        }
        
        public async Task HandleEventAsync(InitialSyncFinishedEvent eventData)
        {
            await _crossChainService.FinishInitialSyncAsync();
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _crossChainService.UpdateCrossChainDataWithLibAsync(eventData.BlockHash, eventData.BlockHeight);
        }
    }
}