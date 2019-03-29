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
    public class GrpcCrossChainClientNodePlugin : INodePlugin, ILocalEventHandler<GrpcServeNewChainReceivedEvent>, ILocalEventHandler<BestChainFoundEventData>
    {
        private readonly CrossChainGrpcClientController _crossChainGrpcClientController;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly ICertificateStore _certificateStore;
        public GrpcCrossChainClientNodePlugin(CrossChainGrpcClientController crossChainGrpcClientController, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, ICertificateStore certificateStore)
        {
            _crossChainGrpcClientController = crossChainGrpcClientController;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
            _certificateStore = certificateStore;
        }

        public Task StartAsync(int chainId)
        {
            if (!_grpcCrossChainConfigOption.LocalClient) 
                return Task.CompletedTask;
            var certificate = LoadCertificate(_grpcCrossChainConfigOption.RemoteParentCertificateFileName);
            return _crossChainGrpcClientController.CreateClient(new GrpcCrossChainCommunicationContext
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                RemoteIsSideChain = false,
                TargetIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                TargetPort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                LocalChainId = chainId,
                CertificateFileName = _grpcCrossChainConfigOption.RemoteParentCertificateFileName,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort
            }, certificate);
        }

        public Task HandleEventAsync(GrpcServeNewChainReceivedEvent receivedEventData)
        {
            GrpcCrossChainCommunicationContext grpcCrossChainCommunicationContext =
                (GrpcCrossChainCommunicationContext) receivedEventData.CrossChainCommunicationContextDto;
            grpcCrossChainCommunicationContext.LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort;
            return _crossChainGrpcClientController.CreateClient(grpcCrossChainCommunicationContext,
                LoadCertificate(grpcCrossChainCommunicationContext.CertificateFileName));
        }
        public Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            _crossChainGrpcClientController.RequestCrossChainIndexing();
            return Task.CompletedTask;
        }
        
        public Task ShutdownAsync()
        {
            _crossChainGrpcClientController.CloseClientsToSideChain();
            _crossChainGrpcClientController.CloseClientToParentChain();
            return Task.CompletedTask;
        }

        private string LoadCertificate(string fileName)
        {
            return _certificateStore.LoadCertificate(fileName);
        }
    }
}