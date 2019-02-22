using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Crosschain.Exceptions;
using AElf.Cryptography.Certificate;
using Grpc.Core;

namespace AElf.Crosschain.Grpc.Client
{
    public class GrpcCrossChainDataProducerConsumerService : ICrossChainDataProducerConsumerService
    {
        private readonly Dictionary<int, GrpcSideChainBlockInfoRpcClient> _clientsToSideChains =
            new Dictionary<int, GrpcSideChainBlockInfoRpcClient>();

        private GrpcParentChainBlockInfoRpcClient _grpcParentChainBlockInfoRpcClient;
        private CertificateStore _certificateStore;
        private CancellationTokenSource _tokenSourceToSideChain;
        private CancellationTokenSource _tokenSourceToParentChain;

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

        public (ICrossChainDataConsumer, ICrossChainDataProducer) CreateConsumerProducer(CommunicationContextDto communicationContextDto)
        {
            var producer = new CrossChainDataProducer
            {
                BlockInfoCache = communicationContextDto.BlockInfoCache,
                ChainId = communicationContextDto.CrossChainCommunicationContext.ChainId,
                TargetChainHeight = communicationContextDto.TargetHeight
            };
            
            var consumer = new CrossChainDataConsumer
            {
                BlockInfoCache = communicationContextDto.BlockInfoCache,
                ChainId = communicationContextDto.CrossChainCommunicationContext.ChainId
            };
            var client =
                CreateGrpcClientAsync(
                    (GrpcCrossChainCommunicationContext) communicationContextDto.CrossChainCommunicationContext,
                    producer, communicationContextDto.IsSideChain);
            //client = clientBasicInfo.TargetIsSideChain ? (ClientToSideChain) client : (ClientToParentChain) client;
            return (consumer, producer);
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>    
        private async Task<IGrpcCrossChainClient> CreateGrpcClientAsync(GrpcCrossChainCommunicationContext crossChainCommunicationContext, 
            CrossChainDataProducer producer, bool isSideChain)
        {
            var channel = CreateChannel(crossChainCommunicationContext.ToUriStr(), crossChainCommunicationContext.TargetChainId);

            if (isSideChain)
            {
                var clientToSideChain = new GrpcSideChainBlockInfoRpcClient(channel, producer);
                _clientsToSideChains.Add(crossChainCommunicationContext.TargetChainId, clientToSideChain);
                await clientToSideChain.StartDuplexStreamingCall(crossChainCommunicationContext.ChainId, _tokenSourceToSideChain.Token);
                return clientToSideChain;
            }

            _grpcParentChainBlockInfoRpcClient = new GrpcParentChainBlockInfoRpcClient(channel, producer);
            await _grpcParentChainBlockInfoRpcClient.StartDuplexStreamingCall(crossChainCommunicationContext.ChainId, _tokenSourceToParentChain.Token);
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


        /// <summary>
        /// Close and clear clients to side chain
        /// </summary>
        public void CloseClientsToSideChain()
        {
            _tokenSourceToSideChain?.Cancel();
            _tokenSourceToSideChain?.Dispose();

            //Todo: probably not needed
            _clientsToSideChains.Clear();
        }

        /// <summary>
        /// close and clear clients to parent chain
        /// </summary>
        public void CloseClientToParentChain()
        {
            _tokenSourceToParentChain?.Cancel();
            _tokenSourceToParentChain?.Dispose();

            //Todo: probably not needed
            _grpcParentChainBlockInfoRpcClient = null;
        }
    }
}