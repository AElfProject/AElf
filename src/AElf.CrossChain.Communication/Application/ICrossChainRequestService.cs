using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainRequestService
    {
        Task RequestCrossChainDataFromOtherChainsAsync();
//        Task ConnectWithNewChainAsync(ICrossChainClientDto crossChainClientDto);

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
            foreach (var chainId in chainIds)
            {
                var client = await _crossChainClientProvider.GetClientAsync(chainId);
                if (client == null)
                    continue;
                Logger.LogTrace($" {ChainHelpers.ConvertChainIdToBase58(chainId)}");
                var targetHeight = _chainCacheEntityProvider.GetChainCacheEntity(chainId).TargetChainHeight();
                _ = _crossChainClientProvider.RequestAsync(client, c => c.RequestCrossChainDataAsync(targetHeight));
            }
        }

//        public async Task ConnectWithNewChainAsync(ICrossChainClientDto crossChainClientDto)
//        {
//            await _crossChainClientProvider.CreateAndCacheClientAsync(crossChainClientDto);
//        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var client = _crossChainClientProvider.CreateClientForChainInitializationData(chainId);
            var chainInitializationData =
                await _crossChainClientProvider.RequestAsync(client,
                    c => c.RequestChainInitializationContext(chainId));
            return chainInitializationData;
        }
    }
}