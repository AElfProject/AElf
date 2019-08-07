using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Application
{
    public class CrossChainClientService : ICrossChainClientService, ITransientDependency
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;

        public ILogger<CrossChainClientService> Logger { get; set; }

        public CrossChainClientService(ICrossChainClientProvider crossChainClientProvider, 
            IBlockCacheEntityProducer blockCacheEntityProducer)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _blockCacheEntityProducer = blockCacheEntityProducer;
        }

        public async Task<ChainInitializationData> RequestChainInitializationData(int chainId)
        {
            var crossChainClientDto = new CrossChainClientDto
            {
                IsClientToParentChain = true,
                LocalChainId = chainId
            };
            var client = _crossChainClientProvider.CreateCrossChainClient(crossChainClientDto);
            return await client.RequestChainInitializationDataAsync(chainId);
        }

        public async Task RequestCrossChainDataAsync(int chainId, long targetHeight)
        {
            if (!_crossChainClientProvider.TryGetClient(chainId, out var client))
                return;
            await ConnectAsync(client);
            if (!client.IsConnected)
                return;
            await client.RequestCrossChainDataAsync(targetHeight);
        }

        public async Task CreateClientAsync(CrossChainClientDto crossChainClientDto)
        {
            var crossChainClient = _crossChainClientProvider.AddOrUpdateClient(crossChainClientDto);
            crossChainClient.SetCrossChainBlockDataEntityHandler(b =>
                _blockCacheEntityProducer.TryAddBlockCacheEntity(b));
            _ = ConnectAsync(crossChainClient);
        }

        public async Task CloseClientsAsync()
        {
            var clientList = _crossChainClientProvider.GetAllClients();
            foreach (var client in clientList)
            {
                await client.CloseAsync();
            }
        }
        
        private async Task ConnectAsync(ICrossChainClient client)
        {
            if (client.IsConnected)
                return;
            Logger.LogTrace($"Try connect with chain {ChainHelper.ConvertChainIdToBase58(client.RemoteChainId)}");
            await client.ConnectAsync();
        }
    }
}