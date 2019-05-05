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

        public async Task CreateClient(ICrossChainCommunicationContext crossChainCommunicationContext)
        {
            if(_grpcCrossChainClients.ContainsKey(crossChainCommunicationContext.RemoteChainId))
                return;
            if (crossChainCommunicationContext.RemoteIsSideChain && 
                !_crossChainMemoryCacheService.GetCachedChainIds().Contains(crossChainCommunicationContext.RemoteChainId)) 
                return; // dont create client for not cached remote side chain
            var client = CreateGrpcClient((GrpcCrossChainCommunicationContext)crossChainCommunicationContext);
            Logger.LogTrace(
                $"Try shake with chain {ChainHelpers.ConvertChainIdToBase58(crossChainCommunicationContext.RemoteChainId)}");
            var reply = await RequestAsync(client, c => c.TryHandShakeAsync(crossChainCommunicationContext.LocalChainId,
                ((GrpcCrossChainCommunicationContext) crossChainCommunicationContext).LocalListeningPort));
            if (reply == null || !reply.Result)
                return;
            _grpcCrossChainClients[crossChainCommunicationContext.RemoteChainId] = client;
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
        private CrossChainGrpcClient CreateGrpcClient(GrpcCrossChainCommunicationContext grpcClientBase)
        {
            string uri = grpcClientBase.ToUriStr();

            if (!grpcClientBase.RemoteIsSideChain)
                return new GrpcClientForParentChain(uri, grpcClientBase.LocalChainId, grpcClientBase.ConnectionTimeout);
            var clientToSideChain = new GrpcClientForSideChain(uri, grpcClientBase.ConnectionTimeout);
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
        
        /// <summary>
        /// Close and clear clients to side chain
        /// </summary>
        public void CloseClientsToSideChain()
        {
            //TokenSourceToSideChain?.Cancel();
            //TokenSourceToSideChain?.Dispose();
            throw new NotImplementedException();
        }

        /// <summary>
        /// close and clear clients to parent chain
        /// </summary>
        public void CloseClientToParentChain()
        {
            //TokenSourceToParentChain?.Cancel();
            //TokenSourceToParentChain?.Dispose();
            throw new NotImplementedException();
        }
    }
}