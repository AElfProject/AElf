using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Grpc;
using AElf.CrossChain.Grpc.Client;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Node.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    public class GrpcCrossChainServerClient : INodePlugin, ILocalEventHandler<GrpcServeNewChainReceivedEvent>, 
        ILocalEventHandler<NewCrossChainCacheEvent>
    {
        private readonly ICrossChainServer _crossChainServer;
        private readonly GrpcClientGenerator _grpcClientGenerator;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly ICertificateStore _certificateStore;
        private readonly int _chainId;
        public GrpcCrossChainServerClient(ICrossChainServer crossChainServer, GrpcClientGenerator grpcClientGenerator, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, ICertificateStore certificateStore, IOptionsSnapshot<ChainOptions> chainOptions)
        {
            _crossChainServer = crossChainServer;
            _grpcClientGenerator = grpcClientGenerator;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
            _certificateStore = certificateStore;
            _chainId = chainOptions.Value.ChainId;
        }

        public Task StartAsync()
        {
            if (_grpcCrossChainConfigOption.LocalServer)
            {
                var keySore = LoadKeyStore(_grpcCrossChainConfigOption.LocalCertificateFileName);
                var cert = LoadCertificate(_grpcCrossChainConfigOption.LocalCertificateFileName);
                _crossChainServer.StartAsync(_grpcCrossChainConfigOption.LocalServerIP,
                    _grpcCrossChainConfigOption.LocalServerPort, new KeyCertificatePair(cert, keySore));
            }

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(GrpcServeNewChainReceivedEvent receivedEventData)
        {
//            if(!_crossChainCommunicationContexts.ContainsKey(receivedEventData.CrossChainCommunicationContextDto.RemoteChainId))
//                _crossChainCommunicationContexts.Add(receivedEventData.CrossChainCommunicationContextDto.RemoteChainId,
//                (GrpcCrossChainCommunicationContext) receivedEventData.CrossChainCommunicationContextDto);
            _grpcClientGenerator.CreateClient(receivedEventData.CrossChainCommunicationContextDto,
                LoadCertificate(
                    ((GrpcCrossChainCommunicationContext) receivedEventData.CrossChainCommunicationContextDto)
                    .CertificateFileName));
            return Task.CompletedTask;
        }
        
        public Task HandleEventAsync(NewCrossChainCacheEvent eventData)
        {
            if(_grpcCrossChainConfigOption.LocalClient && 
               eventData.ChainId == ChainHelpers.ConvertBase58ToChainId(_crossChainConfigOption.ParentChainId))
            {
                var certificate = LoadCertificate(_grpcCrossChainConfigOption.RemoteParentCertificateFileName);
                _grpcClientGenerator.CreateClient(new GrpcCrossChainCommunicationContext
                {
                    RemoteChainId = eventData.ChainId,
                    RemoteIsSideChain = false,
                    TargetIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                    TargetPort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                    LocalChainId = _chainId,
                    CertificateFileName = _grpcCrossChainConfigOption.RemoteParentCertificateFileName,
                    LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort
                }, certificate);
            }
        }
        
        public Task ShutdownAsync()
        {
            _crossChainServer.Dispose();
            _grpcClientGenerator.CloseClientsToSideChain();
            _grpcClientGenerator.CloseClientToParentChain();
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