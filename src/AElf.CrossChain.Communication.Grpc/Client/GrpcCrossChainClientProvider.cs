using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public ICrossChainClient CreateAndCacheClient(CrossChainClientDto crossChainClientDto)
        {
            var chainId = crossChainClientDto.RemoteChainId;
            var uriStr = crossChainClientDto.IsClientToParentChain
                ? GetUriStr(_grpcCrossChainConfigOption.RemoteParentChainServerHost,
                    _grpcCrossChainConfigOption.RemoteParentChainServerPort)
                : GetUriStr(crossChainClientDto.RemoteServerHost, crossChainClientDto.RemoteServerPort);
            
            if (TryGetClient(chainId, out var client) && client.TargetUriString.Equals(uriStr))
                return client; // client already cached
            
            client = CreateCrossChainClient(crossChainClientDto);
            _grpcCrossChainClients.TryAdd(chainId, client);
            Logger.LogTrace("Create client finished.");
            return client;
        }
        
        /// <summary>
        /// Create a new client to another chain.
        /// </summary>
        /// <returns>
        /// </returns>
        public ICrossChainClient CreateCrossChainClient(CrossChainClientDto crossChainClientDto)
        {
            var clientInitializationContext = new GrpcClientInitializationContext
            {
                DialTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalChainId = crossChainClientDto.LocalChainId,
                LocalServerPort = _grpcCrossChainConfigOption.LocalServerPort,
                LocalServerHost = _grpcCrossChainConfigOption.LocalServerHost,
                RemoteChainId = crossChainClientDto.RemoteChainId
            };
            if (crossChainClientDto.IsClientToParentChain)
            {
                clientInitializationContext.UriStr = GetUriStr(_grpcCrossChainConfigOption.RemoteParentChainServerHost,
                    _grpcCrossChainConfigOption.RemoteParentChainServerPort);
                return new ClientForParentChain(clientInitializationContext, _blockCacheEntityProducer);
            }

            clientInitializationContext.UriStr = GetUriStr(crossChainClientDto.RemoteServerHost,
                crossChainClientDto.RemoteServerPort);
            return new ClientForSideChain(clientInitializationContext, _blockCacheEntityProducer);
        }

        #endregion Create client
        
        public bool TryGetClient(int chainId, out ICrossChainClient client)
        {
            return _grpcCrossChainClients.TryGetValue(chainId, out client);
        }
        
        public List<ICrossChainClient> GetAllClients()
        {
            return _grpcCrossChainClients.Values.ToList();
        }

        private string GetUriStr(string host, int port)
        {
            return new UriBuilder("http", host, port).Uri.Authority;
        }
    }
}