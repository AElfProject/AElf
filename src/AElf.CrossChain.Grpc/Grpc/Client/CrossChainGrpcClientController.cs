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

//        public async Task CreateClient(GrpcCrossChainCommunicationDto crossChainCommunicationDto)
//        {
//            if(_grpcCrossChainClients.ContainsKey(crossChainCommunicationDto.RemoteChainId))
//                return;
//            if (!crossChainCommunicationDto.IsClientToParentChain && 
//                !_crossChainMemoryCacheService.GetCachedChainIds().Contains(crossChainCommunicationDto.RemoteChainId)) 
//                return; // dont create client for not cached remote side chain
//            var client = CreateGrpcClient(crossChainCommunicationDto);
//            Logger.LogTrace(
//                $"Try shake with chain {ChainHelpers.ConvertChainIdToBase58(crossChainCommunicationDto.RemoteChainId)}");
//            var reply = await RequestAsync(client,
//                c => c.HandShakeAsync(crossChainCommunicationDto.LocalChainId,
//                    crossChainCommunicationDto.LocalListeningPort));
//            if (reply == null || !reply.Result)
//                return;
//            _grpcCrossChainClients[crossChainCommunicationDto.RemoteChainId] = client;
//        }

//        public async Task CreateClientToSideChain(GrpcCrossChainCommunicationDto crossChainCommunicationDto)
//        {
//            int chainId = crossChainCommunicationDto.RemoteChainId;
//            if (IsClientCreated(chainId) || !IsChainCached(chainId))
//                return;
//            var client = CreateGrpcClient(crossChainCommunicationDto, false);
//            var handShakeResult = await TryHandShakeAsync(client, chainId);
//            if (!handShakeResult)
//            {
//                return;
//            }
//            
//            _grpcCrossChainClients[crossChainCommunicationDto.RemoteChainId] = client;
//        }
        
        public async Task CreateOrUpdateClient(GrpcCrossChainCommunicationDto crossChainCommunicationDto, bool isClientToParentChain)
        {
            var chainId = crossChainCommunicationDto.RemoteChainId;
            var uriStr = crossChainCommunicationDto.ToUriStr();
            var localChainId = crossChainCommunicationDto.LocalChainId;
            var connectionTimeout = crossChainCommunicationDto.ConnectionTimeout;
            var localListeningPort = crossChainCommunicationDto.LocalListeningPort;
            if (IsClientCreated(chainId, uriStr) || !IsAlreadyCachedChain(chainId, isClientToParentChain))
                return;
            var client = CreateGrpcClient(uriStr, localChainId, connectionTimeout, isClientToParentChain);
            var handShakeResult = await TryHandShakeAsync(client, chainId, localListeningPort);
            if (!handShakeResult)
            {
                return;
            }
            
            _grpcCrossChainClients[crossChainCommunicationDto.RemoteChainId] = client;
        }
        
        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>
        private CrossChainGrpcClient CreateGrpcClient(string uriStr, int localChainId, int connectionTimeout, bool isClientToParentChain)
        {
            if (isClientToParentChain)
                return new GrpcClientForParentChain(uriStr, localChainId, connectionTimeout);
            var clientToSideChain = new GrpcClientForSideChain(uriStr, localChainId, connectionTimeout);
            return clientToSideChain;
        }

        private bool IsClientCreated(int chainId, string uriStr)
        {
            return _grpcCrossChainClients.TryGetValue(chainId, out var client) && client.Target.Equals(uriStr);
        }

        private bool IsAlreadyCachedChain(int chainId, bool isClientToParentChain)
        {
            // dont create client for not cached remote side chain
            return isClientToParentChain || _crossChainMemoryCacheService.GetCachedChainIds().Contains(chainId);
        }
        
        #endregion Create client

        #region Request cross chain indexing

        public void RequestCrossChainIndexing(int localListeningPort)
        {
            //Logger.LogTrace("Request cross chain indexing ..");
            var chainIds = _crossChainMemoryCacheService.GetCachedChainIds();
            foreach (var chainId in chainIds)
            {
                if(!_grpcCrossChainClients.TryGetValue(chainId, out var client))
                    continue;
                Logger.LogTrace($"Request chain {ChainHelpers.ConvertChainIdToBase58(chainId)}");
                var targetHeight = _crossChainMemoryCacheService.GetNeededChainHeightForCache(chainId);
                Request(client, c => c.StartIndexingRequest(chainId, targetHeight, _crossChainDataProducer, localListeningPort));
            }
        }
        
        private async Task<bool> TryHandShakeAsync(CrossChainGrpcClient client, int chainId, int localListeningPort)
        {
            Logger.LogTrace(
                $"Try shake with chain {ChainHelpers.ConvertChainIdToBase58(chainId)}");
            var reply = await RequestAsync(client,
                c => c.HandShakeAsync(chainId, localListeningPort));
            return reply != null && reply.Result;
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
        
        public async Task<ChainInitializationContext> RequestChainInitializationContext(string uri, int chainId, int timeout)
        {
            var clientForParentChain = new GrpcClientForParentChain(uri, chainId, timeout);
            var chainInitializationContext = await RequestAsync(clientForParentChain, 
                c => c.RequestChainInitializationContext(chainId));
            return chainInitializationContext;
        }
        
        public async Task CloseClients()
        {
            foreach (var client in _grpcCrossChainClients.Values)
            {
                await client.Close();
            }
        }
    }
}