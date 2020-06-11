using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcCrossChainClientProvider : ICrossChainClientProvider, ISingletonDependency
    {
        public ILogger<GrpcCrossChainClientProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<int, ICrossChainClient> _grpcCrossChainClients =
            new ConcurrentDictionary<int, ICrossChainClient>();

        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcCrossChainClientProvider(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption)
        {
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
        }

        #region Create client

        public ICrossChainClient AddOrUpdateClient(CrossChainClientCreationContext crossChainClientCreationContext)
        {
            var chainId = crossChainClientCreationContext.RemoteChainId;
            var uriStr = crossChainClientCreationContext.IsClientToParentChain
                ? GetUriStr(_grpcCrossChainConfigOption.ParentChainServerIp,
                    _grpcCrossChainConfigOption.ParentChainServerPort)
                : GetUriStr(crossChainClientCreationContext.RemoteServerHost, crossChainClientCreationContext.RemoteServerPort);

            if (TryGetClient(chainId, out var client) && client.TargetUriString.Equals(uriStr))
                return client; // client already cached

            client = CreateClient(crossChainClientCreationContext);
            _grpcCrossChainClients.TryAdd(chainId, client);
            Logger.LogTrace("Create client finished.");
            return client;
        }

        /// <summary>
        /// Create a new client to another chain.
        /// </summary>
        /// <returns>
        /// </returns>
        public ICrossChainClient CreateChainInitializationDataClient(CrossChainClientCreationContext crossChainClientCreationContext)
        {
            return CreateClient(crossChainClientCreationContext);
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

        private ICrossChainClient CreateClient(CrossChainClientCreationContext crossChainClientCreationContext)
        {
            var clientInitializationContext = new GrpcClientInitializationContext
            {
                DialTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalChainId = crossChainClientCreationContext.LocalChainId,
                ListeningPort = _grpcCrossChainConfigOption.ListeningPort,
                RemoteChainId = crossChainClientCreationContext.RemoteChainId
            };
            if (crossChainClientCreationContext.IsClientToParentChain)
            {
                clientInitializationContext.UriStr = GetUriStr(_grpcCrossChainConfigOption.ParentChainServerIp,
                    _grpcCrossChainConfigOption.ParentChainServerPort);
                return new ClientForParentChain(clientInitializationContext);
            }

            clientInitializationContext.UriStr = GetUriStr(crossChainClientCreationContext.RemoteServerHost,
                crossChainClientCreationContext.RemoteServerPort);
            return new ClientForSideChain(clientInitializationContext);
        }
    }
}