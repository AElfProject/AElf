using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.CrossChain.EventMessage;
using AElf.CrossChain.Grpc.Exceptions;
using AElf.Cryptography.Certificate;
using Grpc.Core;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcClientGenerator : ISingletonDependency, ILocalEventHandler<NewChainEvent>
    {
        private readonly Dictionary<int, GrpcSideChainBlockInfoRpcClient> _clientsToSideChains =
            new Dictionary<int, GrpcSideChainBlockInfoRpcClient>();

        private GrpcParentChainBlockInfoRpcClient _grpcParentChainBlockInfoRpcClient;
        private CertificateStore _certificateStore;
        private CancellationTokenSource _tokenSourceToSideChain;
        private CancellationTokenSource _tokenSourceToParentChain;
        private readonly CrossChainDataProducer _crossChainDataProducer;

        public GrpcClientGenerator(CrossChainDataProducer crossChainDataProducer)
        {
            _crossChainDataProducer = crossChainDataProducer;
        }

        /// <summary>
        /// Initialize client manager.
        /// </summary>
        /// <param name="dir"></param>
        public void Init(string dir)
        {
            _certificateStore = new CertificateStore(dir);
            _tokenSourceToSideChain = new CancellationTokenSource();
            _tokenSourceToParentChain = new CancellationTokenSource();
        }

        /// <summary>
        /// Extend interval for request after initial block synchronization.
        /// </summary>
        public void UpdateRequestInterval(int interval)
        {
            _grpcParentChainBlockInfoRpcClient?.UpdateRequestInterval(interval);
            _clientsToSideChains.AsParallel().ToList().ForEach(kv => { kv.Value.UpdateRequestInterval(interval); });
        }

        #region Create client

        public void CreateClient(ICrossChainCommunicationContext crossChainCommunicationContext)
        {
            var client = CreateGrpcClient((GrpcCrossChainCommunicationContext)crossChainCommunicationContext);
            //client = clientBasicInfo.TargetIsSideChain ? (ClientToSideChain) client : (ClientToParentChain) client;
            client.StartDuplexStreamingCall(crossChainCommunicationContext.ChainId, crossChainCommunicationContext.IsSideChain
                ? _tokenSourceToSideChain.Token
                : _tokenSourceToParentChain.Token);
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>    
        private IGrpcCrossChainClient CreateGrpcClient(GrpcCrossChainCommunicationContext grpcClientBase)
        {
            var channel = CreateChannel(grpcClientBase.ToUriStr(), grpcClientBase.ChainId);

            if (grpcClientBase.IsSideChain)
            {
                var clientToSideChain = new GrpcSideChainBlockInfoRpcClient(channel, _crossChainDataProducer);
                _clientsToSideChains.Add(grpcClientBase.ChainId, clientToSideChain);
                return clientToSideChain;
            }

            _grpcParentChainBlockInfoRpcClient = new GrpcParentChainBlockInfoRpcClient(channel, _crossChainDataProducer);
            return _grpcParentChainBlockInfoRpcClient;
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
            string crt = _certificateStore.GetCertificate(targetChainId.ToString());
            if (crt == null)
                throw new CertificateException("Unable to load Certificate.");
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            return channel;
        }

        #endregion

        public Task HandleEventAsync(NewChainEvent eventData)
        {
//            var dto = new CommunicationContextDto
//            {
//                CrossChainCommunicationContext = eventData.CrossChainCommunicationContext,
//                BlockInfoCache = blockInfoCache,
//                TargetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(eventData.LocalChainId, 
//                    eventData.CrossChainCommunicationContext.ChainId, TODO, TODO)
//            };
//            var (consumer, _) = _crossChainDataProducerConsumerService.CreateConsumerProducer(dto);
//            if (eventData.CrossChainCommunicationContext.IsSideChain)
//                _consumers.Add(eventData.CrossChainDataProducer.ChainId, consumer);
//            else
//                ParentChainBlockDataConsumer = consumer;
            CreateClient(eventData.CrossChainCommunicationContext);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close and clear clients to side chain
        /// </summary>
        public void CloseClientsToSideChain()
        {
            _tokenSourceToSideChain?.Cancel();
            _tokenSourceToSideChain?.Dispose();

            _clientsToSideChains.Clear();
        }

        /// <summary>
        /// close and clear clients to parent chain
        /// </summary>
        public void CloseClientToParentChain()
        {
            _tokenSourceToParentChain?.Cancel();
            _tokenSourceToParentChain?.Dispose();

            _grpcParentChainBlockInfoRpcClient = null;
        }
    }
}