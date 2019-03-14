using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Exception;
using AElf.CrossChain.Grpc.Exceptions;
using AElf.CrossChain.Grpc.Server;
using AElf.Cryptography.Certificate;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc.Client
{
    public class CrossChainGrpcClientController : ISingletonDependency
    {
//        private CancellationTokenSource TokenSourceToSideChain { get; } = new CancellationTokenSource();
//        private CancellationTokenSource TokenSourceToParentChain { get; } = new CancellationTokenSource();
        private readonly ICrossChainDataProducer _crossChainDataProducer;
        public ILogger<CrossChainGrpcClientController> Logger { get; set; }

        private ILocalEventBus LocalEventBus { get; }

        private readonly Dictionary<int, IGrpcCrossChainClient> _grpcCrossChainClients = new Dictionary<int, IGrpcCrossChainClient>();
        
        public CrossChainGrpcClientController(ICrossChainDataProducer crossChainDataProducer)
        {
            _crossChainDataProducer = crossChainDataProducer;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        /// <summary>
        /// Extend interval for request after initial block synchronization.
        /// </summary>
        public void UpdateRequestInterval(int interval)
        {
            // no wait
            LocalEventBus.PublishAsync(new GrpcClientRequestIntervalUpdateEvent
            {
                Interval = interval
            });
        }

        #region Create client

        public async Task CreateClient(ICrossChainCommunicationContext crossChainCommunicationContext, string certificate)
        {
            if (!_crossChainDataProducer.GetCachedChainIds().Contains(crossChainCommunicationContext.RemoteChainId)) // dont create client for not cached remote chain
                return;
            var client = CreateGrpcClient((GrpcCrossChainCommunicationContext)crossChainCommunicationContext, certificate);
            var reply = await TryRequest(client, c => c.TryHandShakeAsync(crossChainCommunicationContext.LocalChainId,
                ((GrpcCrossChainCommunicationContext) crossChainCommunicationContext).LocalListeningPort));
            if (reply.Result)
                return;
            _grpcCrossChainClients[crossChainCommunicationContext.RemoteChainId] = client;
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>    
        private IGrpcCrossChainClient CreateGrpcClient(GrpcCrossChainCommunicationContext grpcClientBase, string certificate)
        {
            string uri = grpcClientBase.ToUriStr();

            if (!grpcClientBase.RemoteIsSideChain)
                return new GrpcClientForParentChain(uri, certificate, _crossChainDataProducer);
            var clientToSideChain = new GrpcClientForSideChain(uri, certificate,_crossChainDataProducer);
            return clientToSideChain;
        }

        #endregion Create client

        #region Request cross chain indexing

        public void RequestCrossChainIndexing()
        {
            var chainIds = _crossChainDataProducer.GetCachedChainIds();
            foreach (var chainId in chainIds)
            {
                if(!_grpcCrossChainClients.TryGetValue(chainId, out var client))
                    continue;
                TryRequest(client, c => c.StartIndexingRequest(chainId, _crossChainDataProducer));
            }
        }

        private Task<T> TryRequest<T>(IGrpcCrossChainClient client, Func<IGrpcCrossChainClient, Task<T>> requestFunc)
        {
            try
            {
                return requestFunc(client);
            }
            catch (Exception e) when (e is ChainCacheNotFoundException || e is RpcException)
            {
                Logger.LogWarning(e.ToString());
                return null;
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