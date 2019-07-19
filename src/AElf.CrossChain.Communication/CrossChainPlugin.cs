using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Application;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Communication
{
    public class CrossChainPlugin : INodePlugin, IChainInitializationDataPlugin
    {
        private readonly ICrossChainRequestService _crossChainRequestService;
        private readonly ICrossChainCommunicationController _crossChainCommunicationController;
        
        public CrossChainPlugin(ICrossChainRequestService crossChainRequestService,
            ICrossChainCommunicationController crossChainCommunicationController)
        {
            _crossChainRequestService = crossChainRequestService;
            _crossChainCommunicationController = crossChainCommunicationController;
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var chainInitializationInformation =
                await _crossChainRequestService.RequestChainInitializationDataAsync(chainId);
            return chainInitializationInformation;
        }

        public async Task StartAsync(int chainId)
        {
            await _crossChainCommunicationController.StartAsync(chainId);
        }

        public async Task ShutdownAsync()
        {
            await _crossChainCommunicationController.StopAsync();
        }
    }
}