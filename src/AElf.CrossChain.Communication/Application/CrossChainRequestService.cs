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
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        public ILogger<CrossChainRequestService> Logger { get; set; }

        public CrossChainRequestService(ICrossChainClientProvider crossChainClientProvider, 
            ICrossChainCacheEntityService crossChainCacheEntityService)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _crossChainCacheEntityService = crossChainCacheEntityService;
        }

        public async Task RequestCrossChainDataFromOtherChainsAsync()
        {
            var chainIds = _crossChainCacheEntityService.GetCachedChainIds();
            Logger.LogTrace(
                $"Try to request from chain {string.Join(",", chainIds.Select(ChainHelpers.ConvertChainIdToBase58))}");
            foreach (var chainId in chainIds)
            {
                var client = await _crossChainClientProvider.GetClientAsync(chainId);
                if (client == null)
                    continue;
                var targetHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
                Logger.LogTrace($" Request chain {ChainHelpers.ConvertChainIdToBase58(chainId)} from {targetHeight}");
                _ = _crossChainClientProvider.RequestAsync(client, c => c.RequestCrossChainDataAsync(targetHeight));
            }
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var client = _crossChainClientProvider.CreateClientForChainInitializationData(chainId);
            var chainInitializationData =
                await _crossChainClientProvider.RequestAsync(client,
                    c => c.RequestChainInitializationDataAsync(chainId));
            return chainInitializationData;
        }
    }
}