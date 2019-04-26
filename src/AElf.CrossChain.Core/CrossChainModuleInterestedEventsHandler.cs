using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    public class CrossChainModuleInterestedEventsHandler : ISingletonDependency, ILocalEventHandler<NewIrreversibleBlockFoundEvent>
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;

        public CrossChainModuleInterestedEventsHandler(ICrossChainDataProvider crossChainDataProvider)
        {
            _crossChainDataProvider = crossChainDataProvider;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _crossChainDataProvider.HandleNewLibAsync(eventData);
        }
    }
}