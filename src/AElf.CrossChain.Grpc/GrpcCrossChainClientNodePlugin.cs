using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IChainInitializationPlugin, ILocalEventHandler<GrpcServeNewChainReceivedEvent>, ILocalEventHandler<CrossChainDataValidatedEvent>
    {
        private readonly CrossChainGrpcClientController _crossChainGrpcClientController;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly INewChainRegistrationService _newChainRegistrationService;
        private readonly IBlockchainService _blockchainService;
        private bool _readyToLaunchClient;
        public GrpcCrossChainClientNodePlugin(CrossChainGrpcClientController crossChainGrpcClientController, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, 
            INewChainRegistrationService newChainRegistrationService, IBlockchainService blockchainService)
        {
            _crossChainGrpcClientController = crossChainGrpcClientController;
            _newChainRegistrationService = newChainRegistrationService;
            _blockchainService = blockchainService;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
        }

        public async Task StartAsync(int chainId)
        {
            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.RemoteParentChainNodeIp) 
                || _grpcCrossChainConfigOption.LocalServerPort == 0) 
                return;
            var libIdHeight = await _blockchainService.GetLibHashAndHeight();
            
            if (libIdHeight.BlockHeight > Constants.GenesisBlockHeight)
            {
                // start cache if the lib is higher than genesis 
                await _newChainRegistrationService.RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
            }
            
            var task = _crossChainGrpcClientController.CreateClient(new GrpcCrossChainCommunicationContext
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                RemoteIsSideChain = false,
                TargetIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                TargetPort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                LocalChainId = chainId,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort,
                ConnectionTimeout = _grpcCrossChainConfigOption.ConnectionTimeout
            });
        }

        public Task HandleEventAsync(GrpcServeNewChainReceivedEvent receivedEventData)
        {
            GrpcCrossChainCommunicationContext grpcCrossChainCommunicationContext =
                (GrpcCrossChainCommunicationContext) receivedEventData.CrossChainCommunicationContextDto;
            grpcCrossChainCommunicationContext.LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort;
            grpcCrossChainCommunicationContext.ConnectionTimeout = _grpcCrossChainConfigOption.ConnectionTimeout;
            return _crossChainGrpcClientController.CreateClient(grpcCrossChainCommunicationContext);
        }
        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            if (!await IsReadyToLaunchClient())
                return;
            _crossChainGrpcClientController.RequestCrossChainIndexing();
        }
        
        public Task ShutdownAsync()
        {
            _crossChainGrpcClientController.CloseClientsToSideChain();
            _crossChainGrpcClientController.CloseClientToParentChain();
            return Task.CompletedTask;
        }

        public async Task<IMessage> RequestChainInitializationContextAsync(int chainId)
        {
            string uri = string.Join(":", _grpcCrossChainConfigOption.RemoteParentChainNodeIp, _grpcCrossChainConfigOption.RemoteParentChainNodePort);
            var chainInitializationContext = await _crossChainGrpcClientController.RequestChainInitializationContext(uri, chainId, _grpcCrossChainConfigOption.ConnectionTimeout);
            return chainInitializationContext;
        }

        private async Task<bool> IsReadyToLaunchClient()
        {
            if (!_readyToLaunchClient)
            {
                var libIdHeight = await _blockchainService.GetLibHashAndHeight();
                _readyToLaunchClient = libIdHeight.BlockHeight > Constants.GenesisBlockHeight;
            }

            return _readyToLaunchClient;
        }
        
    }
}