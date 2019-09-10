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

        #pragma warning disable 1998
        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            _ = _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }
    }
}