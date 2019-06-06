using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Communication
{
    public class CrossChainPlugin : INodePlugin, IChainInitializationDataPlugin
    {
        private readonly ICrossChainRequestService _crossChainRequestService;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly ICrossChainCommunicationController _crossChainCommunicationController;
        private readonly IBlockchainService _blockchainService;

        public CrossChainPlugin(ICrossChainRequestService crossChainRequestService, 
            ICrossChainCacheEntityService crossChainCacheEntityService, IBlockchainService blockchainService, 
            ICrossChainCommunicationController crossChainCommunicationController)
        {
            _crossChainRequestService = crossChainRequestService;
            _crossChainCacheEntityService = crossChainCacheEntityService;
            _blockchainService = blockchainService;
            _crossChainCommunicationController = crossChainCommunicationController;
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var chainInitializationInformation =
                await _crossChainRequestService.GetChainInitializationDataAsync(chainId);
            return chainInitializationInformation;
        }

        public async Task StartAsync(int chainId)
        {
            var libIdHeight = await _blockchainService.GetLibHashAndHeightAsync();

            if (libIdHeight.BlockHeight > Constants.GenesisBlockHeight)
            {
                // start cache if the lib is higher than genesis 
                await _crossChainCacheEntityService.RegisterNewChainsAsync(libIdHeight.BlockHash,
                    libIdHeight.BlockHeight);
            }

            await _crossChainCommunicationController.StartAsync(chainId);
        }

        public async Task ShutdownAsync()
        {
            await _crossChainCommunicationController.StopAsync();
        }
    }
}