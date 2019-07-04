using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using AElf.Kernel.Blockchain.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationEventHandler : ILocalEventHandler<CrossChainDataValidatedEvent>, ISingletonDependency
    {
        private readonly ICrossChainRequestService _crossChainRequestService;
        private readonly IBlockchainService _blockchainService;
        private bool _readyToLaunchClient;
        
        public CrossChainCommunicationEventHandler(ICrossChainRequestService crossChainRequestService, 
            IBlockchainService blockchainService)
        {
            _crossChainRequestService = crossChainRequestService;
            _blockchainService = blockchainService;
        }

        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            if (!await IsReadyToRequestAsync())
                return;
            _ = _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
        }
        
        private async Task<bool> IsReadyToRequestAsync()
        {
            if (!_readyToLaunchClient)
            {
                var libIdHeight = await _blockchainService.GetLibHashAndHeightAsync();
                _readyToLaunchClient = libIdHeight.BlockHeight > Constants.GenesisBlockHeight;
            }

            return _readyToLaunchClient;
        }
    }
}