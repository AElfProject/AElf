using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationEventHandler : ILocalEventHandler<CrossChainDataValidatedEvent>, ITransientDependency
    {
        private readonly ICrossChainRequestService _crossChainRequestService;

        public CrossChainCommunicationEventHandler(ICrossChainRequestService crossChainRequestService)
        {
            _crossChainRequestService = crossChainRequestService;
        }

        public Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            return _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }
    }
}