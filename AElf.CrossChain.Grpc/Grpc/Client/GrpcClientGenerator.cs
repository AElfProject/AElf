using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Grpc.Exceptions;
using AElf.Cryptography.Certificate;
using Grpc.Core;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcClientGenerator : ISingletonDependency, ILocalEventHandler<GrpcServeNewChainReceivedEvent>
    {
        private CancellationTokenSource TokenSourceToSideChain { get; } = new CancellationTokenSource();
        private CancellationTokenSource TokenSourceToParentChain { get; } = new CancellationTokenSource();
        private readonly CrossChainDataProducer _crossChainDataProducer;
        private readonly ICertificateStore _certificateStore;
        private ILocalEventBus LocalEventBus { get; }

        public GrpcClientGenerator(CrossChainDataProducer crossChainDataProducer, ICertificateStore certificateStore)
        {
            _crossChainDataProducer = crossChainDataProducer;
            _certificateStore = certificateStore;
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

        private void CreateClient(ICrossChainCommunicationContext crossChainCommunicationContext)
        {
            var client = CreateGrpcClient((GrpcCrossChainCommunicationContext)crossChainCommunicationContext);
            //client = clientBasicInfo.TargetIsSideChain ? (ClientToSideChain) client : (ClientToParentChain) client;
            client.StartDuplexStreamingCall(crossChainCommunicationContext.ChainId, crossChainCommunicationContext.IsSideChain
                ? TokenSourceToSideChain.Token
                : TokenSourceToParentChain.Token);
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>    
        private IGrpcCrossChainClient CreateGrpcClient(GrpcCrossChainCommunicationContext grpcClientBase)
        {
            var channel = CreateChannel(grpcClientBase.ToUriStr(), grpcClientBase.RemoteChainId);

            if (grpcClientBase.IsSideChain)
            {
                var clientToSideChain = new SideChainGrpcClient(channel, _crossChainDataProducer);
                return clientToSideChain;
            }

            return new ParentChainGrpcClient(channel, _crossChainDataProducer);
        }

        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="targetChainId"></param>
        /// <returns></returns>
        /// <exception cref="CertificateException"></exception>
        private Channel CreateChannel(string uriStr, int targetChainId)
        {
            string crt = _certificateStore.LoadCertificate(ChainHelpers.ConvertChainIdToBase58(targetChainId));
            if (crt == null)
                throw new CertificateException("Unable to load Certificate.");
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            return channel;
        }

        #endregion

        public Task HandleEventAsync(GrpcServeNewChainReceivedEvent receivedEventData)
        {
            CreateClient(receivedEventData.CrossChainCommunicationContext);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close and clear clients to side chain
        /// </summary>
        public void CloseClientsToSideChain()
        {
            TokenSourceToSideChain?.Cancel();
            TokenSourceToSideChain?.Dispose();
        }

        /// <summary>
        /// close and clear clients to parent chain
        /// </summary>
        public void CloseClientToParentChain()
        {
            TokenSourceToParentChain?.Cancel();
            TokenSourceToParentChain?.Dispose();
        }
    }
}