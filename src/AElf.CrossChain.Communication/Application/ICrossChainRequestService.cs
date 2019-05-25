using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainRequestService
    {
        Task RequestCrossChainDataFromOtherChainsAsync();

        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
    }
    
    public class CrossChainRequestService : ICrossChainRequestService
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;
        public ILogger<CrossChainRequestService> Logger { get; set; }

        public CrossChainRequestService(ICrossChainClientProvider crossChainClientProvider, 
            IChainCacheEntityProvider chainCacheEntityProvider)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _chainCacheEntityProvider = chainCacheEntityProvider;
        }

        public async Task RequestCrossChainDataFromOtherChainsAsync()
        {
            var chainIds = _chainCacheEntityProvider.CachedChainIds;
            Logger.LogTrace($"Try to request from chain {string.Join(",", chainIds.Select(ChainHelpers.ConvertChainIdToBase58))}");
            foreach (var chainId in chainIds)
            {
                var client = await _crossChainClientProvider.GetClientAsync(chainId);
                if (client == null)
                    continue;
                var targetHeight = _chainCacheEntityProvider.GetChainCacheEntity(chainId).TargetChainHeight();
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