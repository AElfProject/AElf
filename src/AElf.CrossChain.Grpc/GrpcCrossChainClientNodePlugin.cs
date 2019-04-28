using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IChainInitializationPlugin, ILocalEventHandler<GrpcServeNewChainReceivedEvent>, ILocalEventHandler<CrossChainDataValidatedEvent>
    {
        private readonly CrossChainGrpcClientController _crossChainGrpcClientController;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IBlockchainService _blockchainService;
        public GrpcCrossChainClientNodePlugin(CrossChainGrpcClientController crossChainGrpcClientController, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, 
            ICrossChainDataProvider crossChainDataProvider, IBlockchainService blockchainService)
        {
            _crossChainGrpcClientController = crossChainGrpcClientController;
            _crossChainDataProvider = crossChainDataProvider;
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
            
            if (libIdHeight.BlockHeight > KernelConstants.GenesisBlockHeight)
            {
                // start cache if the lib is higher than genesis 
                await _crossChainDataProvider.RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
            }
            
            var task = _crossChainGrpcClientController.CreateClient(new GrpcCrossChainCommunicationContext
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                RemoteIsSideChain = false,
                TargetIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                TargetPort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                LocalChainId = chainId,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort,
                Timeout = _grpcCrossChainConfigOption.ConnectionTimeout
            });
        }

        public Task HandleEventAsync(GrpcServeNewChainReceivedEvent receivedEventData)
        {
            GrpcCrossChainCommunicationContext grpcCrossChainCommunicationContext =
                (GrpcCrossChainCommunicationContext) receivedEventData.CrossChainCommunicationContextDto;
            grpcCrossChainCommunicationContext.LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort;
            grpcCrossChainCommunicationContext.Timeout = _grpcCrossChainConfigOption.ConnectionTimeout;
            return _crossChainGrpcClientController.CreateClient(grpcCrossChainCommunicationContext);
        }
        public Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
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

        public async Task<ChainInitializationContext> RequestChainInitializationContextAsync(int chainId)
        {
            string uri = string.Join(":", _grpcCrossChainConfigOption.RemoteParentChainNodeIp, _grpcCrossChainConfigOption.RemoteParentChainNodePort);
            var chainInitializationContext = await _crossChainGrpcClientController.RequestChainInitializationContext(uri, chainId, _grpcCrossChainConfigOption.ConnectionTimeout);
            return chainInitializationContext;
        }
    }
}