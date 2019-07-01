using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientProvider : ICrossChainClientProvider, ISingletonDependency
    {
        public ILogger<GrpcCrossChainClientProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<int, ICrossChainClient> _grpcCrossChainClients =
            new ConcurrentDictionary<int, ICrossChainClient>();
        
        private readonly ConcurrentDictionary<int, ICrossChainClient> _connectionFailedClients =
            new ConcurrentDictionary<int, ICrossChainClient>();
        
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;

        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcCrossChainClientProvider(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
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
            var clientInitializationContext = new GrpcClientInitializationContext
            {
                DialTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalChainId = localChainId,
                LocalServerPort = _grpcCrossChainConfigOption.LocalServerPort,
                UriStr = uriStr,
                LocalServerHost = _grpcCrossChainConfigOption.LocalServerHost
            };
            var client = new ClientForParentChain(clientInitializationContext, _blockCacheEntityProducer);
            return client;
        }

        public void CreateAndCacheClient(CrossChainClientDto crossChainClientDto)
        {
            var chainId = crossChainClientDto.RemoteChainId;
            var uriStr = GetUriStr(crossChainClientDto.RemoteServerHost, crossChainClientDto.RemoteServerPort);
            var isClientToParentChain = crossChainClientDto.IsClientToParentChain;
            if (TryGetCachedClient(chainId, out var client) && client.TargetUriString.Equals(uriStr))
                return; // client already cached
            
            var localChainId = crossChainClientDto.LocalChainId;
            client = CreateGrpcClient(uriStr, localChainId, chainId, isClientToParentChain);
            _ = TryConnectAndUpdateClientAsync(client);
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
                var connectionResult = await TryConnectAndUpdateClientAsync(crossChainClient);
                
                return connectionResult ? crossChainClient : null;
            }
            
            return null;
        }
        
        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>
        private ICrossChainClient CreateGrpcClient(string uriStr, int localChainId, int remoteChainId, bool isClientToParentChain)
        {
            var clientInitializationContext = new GrpcClientInitializationContext
            {
                DialTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalChainId = localChainId,
                LocalServerPort = _grpcCrossChainConfigOption.LocalServerPort,
                UriStr = uriStr,
                LocalServerHost = _grpcCrossChainConfigOption.LocalServerHost,
                RemoteChainId = remoteChainId
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
        
        #endregion Create client

        #region Request
        
        private async Task<bool> TryConnectAndUpdateClientAsync(ICrossChainClient client)
        {
            Logger.LogTrace($"Try handshake with chain {ChainHelper.ConvertChainIdToBase58(client.RemoteChainId)}");
            _connectionFailedClients.TryAdd(client.RemoteChainId, client);
            var connectionResult = await RequestAsync(client, c => c.ConnectAsync());
            if (connectionResult)
            {
                Logger.LogTrace($"Connected to chain {ChainHelper.ConvertChainIdToBase58(client.RemoteChainId)}");
                UpdateClient(client);
            }
            
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
                HandleRpcException(client, e);
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
                HandleRpcException(client, e);
            }
        }

        private void HandleRpcException(ICrossChainClient client, RpcException e)
        {
            Logger.LogWarning($"Cross chain grpc request failed with exception {e.Message}");
            MarkConnectionFailedClient(client.RemoteChainId); 
        }

        private void UpdateClient(ICrossChainClient client)
        {
            _connectionFailedClients.TryRemove(client.RemoteChainId, out _);
            _grpcCrossChainClients.TryAdd(client.RemoteChainId, client);
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
    }
}