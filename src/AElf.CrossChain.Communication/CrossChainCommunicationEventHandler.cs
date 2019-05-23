using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationEventHandler : ILocalEventHandler<CrossChainDataValidatedEvent>
    {
        private readonly ICrossChainRequestService _crossChainRequestService;

        public CrossChainCommunicationEventHandler(ICrossChainRequestService crossChainRequestService)
        {
            _crossChainRequestService = crossChainRequestService;
        }

        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            await _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }
    }
}