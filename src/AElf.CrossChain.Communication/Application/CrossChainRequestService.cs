using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Application
{
    public class CrossChainRequestService : ICrossChainRequestService, ITransientDependency
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;

        public ILogger<CrossChainRequestService> Logger { get; set; }

        public CrossChainRequestService(ICrossChainCacheEntityService crossChainCacheEntityService, 
            ICrossChainClientService crossChainClientService)
        {
            _crossChainCacheEntityService = crossChainCacheEntityService;
            _crossChainClientService = crossChainClientService;
        }

        public async Task RequestCrossChainDataFromOtherChainsAsync()
        {
            var chainIds = _crossChainCacheEntityService.GetCachedChainIds();
            Logger.LogTrace(
                $"Try to request from chain {string.Join(",", chainIds.Select(ChainHelper.ConvertChainIdToBase58))}");
            foreach (var chainId in chainIds)
            {
                var client = await _crossChainClientService.GetClientAsync(chainId);
                if (client == null)
                    continue;
                var targetHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
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