using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Application;

namespace AElf.CrossChain.Communication
{
    public class CrossChainPlugin : IChainInitializationDataPlugin
    {
        private readonly ICrossChainRequestService _crossChainRequestService;
        
        public CrossChainPlugin(ICrossChainRequestService crossChainRequestService)
        {
            _crossChainRequestService = crossChainRequestService;
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var chainInitializationInformation =
                await _crossChainRequestService.RequestChainInitializationDataAsync(chainId);
            return chainInitializationInformation;
        }
    }
}