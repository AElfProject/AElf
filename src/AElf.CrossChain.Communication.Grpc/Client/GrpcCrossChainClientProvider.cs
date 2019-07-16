using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Infrastructure;
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
        
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;

        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcCrossChainClientProvider(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IBlockCacheEntityProducer blockCacheEntityProducer)
        {
            _blockCacheEntityProducer = blockCacheEntityProducer;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
        }

        #region Create client

        public ICrossChainClient CreateCrossChainClient(CrossChainClientDto crossChainClientDto)
        {
            var uriStr = GetUriStr(crossChainClientDto.RemoteServerHost, crossChainClientDto.RemoteServerPort);

            var client = CreateGrpcClient(uriStr, crossChainClientDto.LocalChainId, crossChainClientDto.RemoteChainId,
                crossChainClientDto.IsClientToParentChain);

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
            _grpcCrossChainClients.TryAdd(chainId, client);
            ConnectAsync(client);
            Logger.LogTrace("Create client finished.");
        }

        /// <summary>
        /// Return cached client by chain id. Retry handshake if it was marked with connection failed.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<ICrossChainClient> GetClientAsync(int chainId)
        {
            if (!_grpcCrossChainClients.TryGetValue(chainId, out var crossChainClient)) 
                return null;
            if (crossChainClient.IsConnected)
                return crossChainClient;
            // try connect first 
            await ConnectAsync(crossChainClient);
            
            return crossChainClient.IsConnected ? crossChainClient : null;
        }
        
        /// <summary>
        /// Create a new client to another chain.
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
            return _grpcCrossChainClients.TryGetValue(chainId, out client);
        }
        
        #endregion Create client

        #region Request
        
        private Task ConnectAsync(ICrossChainClient client)
        {
            Logger.LogTrace($"Try handshake with chain {ChainHelper.ConvertChainIdToBase58(client.RemoteChainId)}");
            return client.ConnectAsync();
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