using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.Certificate;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainServerClient : INodePlugin, ILocalEventHandler<GrpcServeNewChainReceivedEvent>, ILocalEventHandler<BestChainFoundEventData>
    {
        private readonly ICrossChainServer _crossChainServer;
        private readonly CrossChainGrpcClientController _crossChainGrpcClientController;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly ICertificateStore _certificateStore;
        public GrpcCrossChainServerClient(ICrossChainServer crossChainServer, CrossChainGrpcClientController crossChainGrpcClientController, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, ICertificateStore certificateStore)
        {
            _crossChainServer = crossChainServer;
            _crossChainGrpcClientController = crossChainGrpcClientController;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
            _certificateStore = certificateStore;
        }

        public Task StartAsync(int chainId)
        {
            if (_grpcCrossChainConfigOption.LocalServer)
            {
                var keySore = LoadKeyStore(_grpcCrossChainConfigOption.LocalCertificateFileName);
                var cert = LoadCertificate(_grpcCrossChainConfigOption.LocalCertificateFileName);
                _crossChainServer.StartAsync(_grpcCrossChainConfigOption.LocalServerIP,
                    _grpcCrossChainConfigOption.LocalServerPort, new KeyCertificatePair(cert, keySore));
            }

            if (_grpcCrossChainConfigOption.LocalClient)
            {
                var certificate = LoadCertificate(_grpcCrossChainConfigOption.RemoteParentCertificateFileName);
                var task = _crossChainGrpcClientController.CreateClient(new GrpcCrossChainCommunicationContext
                {
                    RemoteChainId = ChainHelpers.ConvertBase58ToChainId(_crossChainConfigOption.ParentChainId),
                    RemoteIsSideChain = false,
                    TargetIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                    TargetPort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                    LocalChainId = chainId,
                    CertificateFileName = _grpcCrossChainConfigOption.RemoteParentCertificateFileName,
                    LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort
                }, certificate);
            }

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(GrpcServeNewChainReceivedEvent receivedEventData)
        {
            return _crossChainGrpcClientController.CreateClient(receivedEventData.CrossChainCommunicationContextDto,
                LoadCertificate(
                    ((GrpcCrossChainCommunicationContext) receivedEventData.CrossChainCommunicationContextDto)
                    .CertificateFileName));
        }
        public Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            _crossChainGrpcClientController.RequestCrossChainIndexing();
            return Task.CompletedTask;
        }
        
        public Task ShutdownAsync()
        {
            _crossChainServer.Dispose();
            _crossChainGrpcClientController.CloseClientsToSideChain();
            _crossChainGrpcClientController.CloseClientToParentChain();
            return Task.CompletedTask;
        }

        private string LoadCertificate(string fileName)
        {
            return _certificateStore.LoadCertificate(fileName);
        }

        private string LoadKeyStore(string fileName)
        {
            return _certificateStore.LoadKeyStore(fileName);
        }
    }
}