using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Plugin.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
{
    public class GrpcClientProvider : IGrpcCrossChainClientProvider, ISingletonDependency
    {
        public ILogger<GrpcClientProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<int, CrossChainGrpcClient> _grpcCrossChainClients =
            new ConcurrentDictionary<int, CrossChainGrpcClient>();
        
        private readonly ConcurrentDictionary<int, CrossChainGrpcClient> _connectionFailedClients =
            new ConcurrentDictionary<int, CrossChainGrpcClient>();
        
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcClientProvider(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption)
        {
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
        }

        #region Create client
        
//        public async Task CreateOrUpdateClient(GrpcCrossChainClientDto crossChainClientDto, bool isClientToParentChain)
//        {
//            var chainId = crossChainClientDto.RemoteChainId;
//            var uriStr = crossChainClientDto.ToUriStr();
//            var localChainId = crossChainClientDto.LocalChainId;
//            var connectionTimeout = crossChainClientDto.ConnectionTimeout;
//            var localListeningPort = crossChainClientDto.LocalListeningPort;
//            if (IsClientCreated(chainId, uriStr) || !IsAlreadyCachedChain(chainId, isClientToParentChain))
//                return;
//            var client = CreateGrpcClient(uriStr, localChainId, connectionTimeout, isClientToParentChain);
//            var handShakeResult = await TryHandShakeAsync(client, chainId, localListeningPort);
//            if (!handShakeResult)
//            {
//                return;
//            }
//            
//            _grpcCrossChainClients[crossChainClientDto.RemoteChainId] = client;
//        }

        public CrossChainGrpcClient CreateClientForChainInitializationInformation(int chainId)
        {
            var localChainId = chainId;
            var uriStr = new UriBuilder("http", _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                _grpcCrossChainConfigOption.RemoteParentChainServerPort).Uri.Authority;
            var client = new GrpcClientForSideChain(uriStr, localChainId, _grpcCrossChainConfigOption.ConnectionTimeout);
            return client;
        }

        public async Task CreateAndCacheClientAsync(ICrossChainClientDto crossChainClientDto)
        {
            var grpcCrossChainClientDto = (GrpcCrossChainClientDto) crossChainClientDto;
            var chainId = grpcCrossChainClientDto.RemoteChainId;
            var uriStr = grpcCrossChainClientDto.ToUriStr();
            var isClientToParentChain = grpcCrossChainClientDto.IsClientToParentChain;
            if (TryGetCachedClient(chainId, out var client) && client.Target.Equals(uriStr))
                return; // client already cached
            
            var localChainId = grpcCrossChainClientDto.LocalChainId;
            var connectionTimeout = grpcCrossChainClientDto.ConnectionTimeout;
            var localListeningPort = grpcCrossChainClientDto.LocalListeningPort;
            client = CreateGrpcClient(uriStr, localChainId, connectionTimeout, isClientToParentChain);
            var handShakeResult = await TryHandShakeAsync(client, chainId, localListeningPort);
            if (!handShakeResult)
            {
                return;
            }
            
            _grpcCrossChainClients[grpcCrossChainClientDto.RemoteChainId] = client;
        }

        /// <summary>
        /// Return cached client by chain id. Retry handshake if it was marked with connection failed.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<CrossChainGrpcClient> GetClientAsync(int chainId)
        {
            if (_grpcCrossChainClients.TryGetValue(chainId, out var crossChainClient))
                return crossChainClient;

            if (!_connectionFailedClients.TryGetValue(chainId, out crossChainClient)) 
                return null;
            
            // try handshake first 
            var handshakeResult =
                await TryHandShakeAsync(crossChainClient, chainId, _grpcCrossChainConfigOption.LocalServerPort);
            return handshakeResult ? crossChainClient : null;
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

        private bool TryGetCachedClient(int chainId, out CrossChainGrpcClient client)
        {
            return _grpcCrossChainClients.TryGetValue(chainId, out client) ||
                   _connectionFailedClients.TryGetValue(chainId, out client);
        }

        /// <summary>
        /// Mark a client if its connection failed.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        private void MarkConnectionFailedClient(int chainId)
        {
            if (_grpcCrossChainClients.TryRemove(chainId, out var client))
                _connectionFailedClients.AddOrUpdate(chainId, client, (id, c) => client);
        }

//        private bool IsAlreadyCachedChain(int chainId, bool isClientToParentChain)
//        {
//            // dont create client for not cached remote side chain
//            return isClientToParentChain || _chainCacheEntityProvider.CachedChainIds.Contains(chainId);
//        }
        
        #endregion Create client

        #region Request

//        public void RequestCrossChainIndexing(int localListeningPort)
//        {
//            //Logger.LogTrace("Request cross chain indexing ..");
//            var chainIds = _chainCacheEntityProvider.CachedChainIds;
//            foreach (var chainId in chainIds)
//            {
//                if (!_grpcCrossChainClients.TryGetValue(chainId, out var client))
//                    continue;
//                Logger.LogTrace($"Request chain {ChainHelpers.ConvertChainIdToBase58(chainId)}");
//                var targetHeight = _chainCacheEntityProvider.GetChainCacheEntity(chainId).TargetChainHeight();
//                Request(client, c => c.StartIndexingRequest(chainId, targetHeight, _blockCacheEntityProducer, localListeningPort));
//            }
//        }
        
        private async Task<bool> TryHandShakeAsync(CrossChainGrpcClient client, int chainId, int localListeningPort)
        {
            Logger.LogTrace($"Try handshake with chain {ChainHelpers.ConvertChainIdToBase58(chainId)}");
            var reply = await client.HandShakeAsync(localListeningPort);
            return reply != null && reply.Result;
        }

        public async Task<T> RequestAsync<T>(CrossChainGrpcClient client, Func<CrossChainGrpcClient, Task<T>> requestFunc)
        {
            try
            {
                return await requestFunc(client);
            }
            catch (RpcException e)
            {
                HandleFailedException(client.ChainId, e);
                return default(T);
            }
        }

        public async Task RequestAsync(CrossChainGrpcClient client, Func<CrossChainGrpcClient, Task> requestFunc)
        {
            try
            {
                await requestFunc(client);
            }
            catch (RpcException e)
            {
                HandleFailedException(client.ChainId, e);
            }
        }
        
        private void HandleFailedException(int chainId, RpcException e)
        {
            Logger.LogWarning($"Cross chain grpc request failed with exception {e.Message}");
            MarkConnectionFailedClient(chainId); 
        }
        
        #endregion      
        
//        public async Task<SideChainInitializationInformation> RequestChainInitializationContextAsync(string uri, int chainId, int timeout)
//        {
//            var clientForParentChain = new GrpcClientForParentChain(uri, chainId, timeout);
//            var chainInitializationContext = await RequestAsync(clientForParentChain, 
//                c => c.RequestChainInitializationContext(chainId));
//            return chainInitializationContext;
//        }
    }
}