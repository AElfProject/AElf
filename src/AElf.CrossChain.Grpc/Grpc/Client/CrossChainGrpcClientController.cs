using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
{
    public class CrossChainGrpcClientController : ISingletonDependency
    {
        private readonly ICrossChainDataProducer _crossChainDataProducer;
        public ILogger<CrossChainGrpcClientController> Logger { get; set; }

        private readonly ConcurrentDictionary<int, CrossChainGrpcClient> _grpcCrossChainClients =
            new ConcurrentDictionary<int, CrossChainGrpcClient>();
        private readonly ICrossChainMemoryCacheService _crossChainMemoryCacheService;
        public CrossChainGrpcClientController(ICrossChainDataProducer crossChainDataProducer, 
            ICrossChainMemoryCacheService crossChainMemoryCacheService)
        {
            _crossChainDataProducer = crossChainDataProducer;
            _crossChainMemoryCacheService = crossChainMemoryCacheService;
        }

        #region Create client

        public async Task CreateClient(GrpcCrossChainCommunicationDto crossChainCommunicationDto)
        {
            if(_grpcCrossChainClients.ContainsKey(crossChainCommunicationDto.RemoteChainId))
                return;
            if (!crossChainCommunicationDto.IsClientToParentChain && 
                !_crossChainMemoryCacheService.GetCachedChainIds().Contains(crossChainCommunicationDto.RemoteChainId)) 
                return; // dont create client for not cached remote side chain
            var client = CreateGrpcClient(crossChainCommunicationDto);
            Logger.LogTrace(
                $"Try shake with chain {ChainHelpers.ConvertChainIdToBase58(crossChainCommunicationDto.RemoteChainId)}");
            var reply = await RequestAsync(client,
                c => c.HandShakeAsync(crossChainCommunicationDto.LocalChainId,
                    crossChainCommunicationDto.LocalListeningPort));
            if (reply == null || !reply.Result)
                return;
            _grpcCrossChainClients[crossChainCommunicationDto.RemoteChainId] = client;
        }

        public async Task<ChainInitializationContext> RequestChainInitializationContext(string uri, int chainId, int timeout)
        {
            var clientForParentChain = new GrpcClientForParentChain(uri, chainId, timeout);
            var chainInitializationContext = await RequestAsync(clientForParentChain, 
                c => c.RequestChainInitializationContext(chainId));
            return chainInitializationContext;
        }
        
        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>
        private CrossChainGrpcClient CreateGrpcClient(GrpcCrossChainCommunicationDto crossChainCommunicationDto)
        {
            string uri = crossChainCommunicationDto.ToUriStr();

            if (crossChainCommunicationDto.IsClientToParentChain)
                return new GrpcClientForParentChain(uri, crossChainCommunicationDto.LocalChainId, crossChainCommunicationDto.ConnectionTimeout);
            var clientToSideChain = new GrpcClientForSideChain(uri, crossChainCommunicationDto.LocalChainId, crossChainCommunicationDto.ConnectionTimeout);
            return clientToSideChain;
        }

        #endregion Create client

        #region Request cross chain indexing

        public void RequestCrossChainIndexing()
        {
            //Logger.LogTrace("Request cross chain indexing ..");
            var chainIds = _crossChainMemoryCacheService.GetCachedChainIds();
            foreach (var chainId in chainIds)
            {
                if(!_grpcCrossChainClients.TryGetValue(chainId, out var client))
                    continue;
                Logger.LogTrace($"Request chain {ChainHelpers.ConvertChainIdToBase58(chainId)}");
                var targetHeight = _crossChainMemoryCacheService.GetNeededChainHeightForCache(chainId);
                Request(client, c => c.StartIndexingRequest(chainId, targetHeight, _crossChainDataProducer));
            }
        }

        private async Task<T> RequestAsync<T>(CrossChainGrpcClient client, Func<CrossChainGrpcClient, Task<T>> requestFunc)
        {
            try
            {
                return await requestFunc(client);
            }
            catch (RpcException e)
            {
                Logger.LogWarning($"Cross chain grpc request failed with exception {e.Message}");
                return default(T);
            }
        }

        private void Request(CrossChainGrpcClient client, Func<CrossChainGrpcClient, Task> requestFunc)
        {
            try
            {
                requestFunc(client);
            }
            catch (RpcException e)
            {
                Logger.LogWarning($"Cross chain grpc request failed with exception {e.Message}");
            }
        }
        
        #endregion

        public async Task CloseClients()
        {
            foreach (var client in _grpcCrossChainClients.Values)
            {
                await client.Close();
            }
        }
        
    }
}