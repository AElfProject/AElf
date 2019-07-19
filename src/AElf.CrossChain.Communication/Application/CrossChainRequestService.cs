using System.Linq;
using System.Threading.Tasks;
using Acs7;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Application
{
    public class CrossChainRequestService : ICrossChainRequestService, ITransientDependency
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private readonly ICrossChainService _crossChainService;

        public ILogger<CrossChainRequestService> Logger { get; set; }

        public CrossChainRequestService(ICrossChainService crossChainService, 
            ICrossChainClientService crossChainClientService)
        {
            _crossChainService = crossChainService;
            _crossChainClientService = crossChainClientService;
        }

        public async Task RequestCrossChainDataFromOtherChainsAsync()
        {
            var chainIds = _crossChainService.GetRegisteredChainIdList();
            Logger.LogTrace(
                $"Try to request from chain {string.Join(",", chainIds.Select(ChainHelper.ConvertChainIdToBase58))}");
            foreach (var chainId in chainIds)
            {
                var client = await _crossChainClientService.GetClientAsync(chainId);
                if (client == null)
                    continue;
                var targetHeight = _crossChainService.GetNeededChainHeight(chainId);
                Logger.LogTrace($" Request chain {ChainHelper.ConvertChainIdToBase58(chainId)} from {targetHeight}");
                _ = client.RequestCrossChainDataAsync(targetHeight);
            }
        }

        public async Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            var client = _crossChainClientService.CreateClientForChainInitializationData(chainId);
            var chainInitializationData =
                await client.RequestChainInitializationDataAsync(chainId);
            return chainInitializationData;
        }
    }
}