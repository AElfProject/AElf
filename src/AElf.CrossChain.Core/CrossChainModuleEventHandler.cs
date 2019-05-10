using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    internal class CrossChainModuleEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ISingletonDependency
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly INewChainRegistrationService _newChainRegistrationService;

        public CrossChainModuleEventHandler(ICrossChainDataProvider crossChainDataProvider, INewChainRegistrationService newChainRegistrationService)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _newChainRegistrationService = newChainRegistrationService;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _newChainRegistrationService.RegisterNewChainsAsync(eventData.BlockHash, eventData.BlockHeight);
            _crossChainDataProvider.HandleLibEvent(new IrreversibleBlockDto
            {
                BlockHash = eventData.BlockHash,
                BlockHeight = eventData.BlockHeight
            });
        }
    }
}