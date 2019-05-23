using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcClientProvider : ICrossChainClientProvider, ISingletonDependency
    {
        public ILogger<GrpcClientProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<int, ICrossChainClient> _grpcCrossChainClients =
            new ConcurrentDictionary<int, ICrossChainClient>();
        
        private readonly ConcurrentDictionary<int, ICrossChainClient> _connectionFailedClients =
            new ConcurrentDictionary<int, ICrossChainClient>();
        
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;

        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcClientProvider(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IBlockCacheEntityProducer blockCacheEntityProducer)
        {
            _blockCacheEntityProducer = blockCacheEntityProducer;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
        }

        #region Create client

        public ICrossChainClient CreateClientForChainInitializationData(int chainId)
        {
            var localChainId = chainId;

            var uriStr = GetUriStr(_grpcCrossChainConfigOption.RemoteParentChainServerHost,
                _grpcCrossChainConfigOption.RemoteParentChainServerPort);
            var clientInitializationContext = new GrpcCrossChainInitializationContext
            {
                DialTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalChainId = localChainId,
                LocalServerPort = _grpcCrossChainConfigOption.LocalServerPort,
                UriStr = uriStr,
                LocalServerHost = _grpcCrossChainConfigOption.LocalServerHost
            };
            var client = new ClientForSideChain(clientInitializationContext, _blockCacheEntityProducer);
            return client;
        }

        public async Task CreateAndCacheClientAsync(ICrossChainClientDto crossChainClientDto)
        {
            var chainId = crossChainClientDto.RemoteChainId;
            var uriStr = GetUriStr(crossChainClientDto.RemoteServerHost, crossChainClientDto.RemoteServerPort);
            var isClientToParentChain = crossChainClientDto.IsClientToParentChain;
            if (TryGetCachedClient(chainId, out var client) && client.TargetUriString.Equals(uriStr))
                return; // client already cached
            
            var localChainId = crossChainClientDto.LocalChainId;
            //var connectionTimeout = crossChainClientDto.ConnectionTimeout;
            client = CreateGrpcClient(uriStr, localChainId, isClientToParentChain);
            var handShakeResult = await TryConnectAsync(client);
            if (!handShakeResult)
            {
                return;
            }
            
            _grpcCrossChainClients[crossChainClientDto.RemoteChainId] = client;
        }

        /// <summary>
        /// Return cached client by chain id. Retry handshake if it was marked with connection failed.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<ICrossChainClient> GetClientAsync(int chainId)
        {
            if (_grpcCrossChainClients.TryGetValue(chainId, out var crossChainClient))
                return crossChainClient;

            if (_connectionFailedClients.TryGetValue(chainId, out crossChainClient))
            {
                // try connect first 
                var connectionResult = await TryConnectAsync(crossChainClient);
                return connectionResult ? crossChainClient : null;
            }

            return null;
        }
        
        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>
        private ICrossChainClient CreateGrpcClient(string uriStr, int localChainId, bool isClientToParentChain)
        {
            var clientInitializationContext = new GrpcCrossChainInitializationContext
            {
                DialTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalChainId = localChainId,
                LocalServerPort = _grpcCrossChainConfigOption.LocalServerPort,
                UriStr = uriStr,
                LocalServerHost = _grpcCrossChainConfigOption.LocalServerHost
            };
            if (isClientToParentChain)
                return new ClientForParentChain(clientInitializationContext, _blockCacheEntityProducer);
            return new ClientForSideChain(clientInitializationContext, _blockCacheEntityProducer);
        }

        private bool TryGetCachedClient(int chainId, out ICrossChainClient client)
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
        
        private async Task<bool> TryConnectAsync(ICrossChainClient client)
        {
            Logger.LogTrace($"Try handshake with chain {ChainHelpers.ConvertChainIdToBase58(client.ChainId)}");
            var connectionResult = await client.ConnectAsync();
            return connectionResult;
        }

        public async Task<T> RequestAsync<T>(ICrossChainClient client, Func<ICrossChainClient, Task<T>> requestFunc)
        {
            try
            {
                return await requestFunc(client);
            }
            catch (RpcException e)
            {
                await HandleRpcException(client, e);
                return default(T);
            }
        }

        public async Task RequestAsync(ICrossChainClient client, Func<ICrossChainClient, Task> requestFunc)
        {
            try
            {
                await requestFunc(client);
            }
            catch (RpcException e)
            {
                await HandleRpcException(client, e);
            }
        }

        
        private async Task HandleRpcException(ICrossChainClient client, RpcException e)
        {
            Logger.LogWarning($"Cross chain grpc request failed with exception {e.Message}");
            var reconnectionResult = await TryConnectAsync(client);
            if (reconnectionResult)
                return;
            MarkConnectionFailedClient(client.ChainId); 
        }

        #endregion      
        
        public async Task CloseClientsAsync()
        {
            foreach (var client in _grpcCrossChainClients.Values)
            {
                await client.CloseAsync();
            }
        }

        private string GetUriStr(string host, int port)
        {
            return new UriBuilder("http", host, port).Uri.Authority;
        }

//        public async Task<SideChainInitializationInformation> RequestChainInitializationContextAsync(string uri, int chainId, int timeout)
//        {
//            var clientForParentChain = new GrpcClientForParentChain(uri, chainId, timeout);
//            var chainInitializationContext = await RequestAsync(clientForParentChain, 
//                c => c.RequestChainInitializationContext(chainId));
//            return chainInitializationContext;
//        }
    }
}