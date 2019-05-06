using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IChainInitializationPlugin, ILocalEventHandler<GrpcCrossChainRequestReceivedEvent>, ILocalEventHandler<CrossChainDataValidatedEvent>
    {
        private readonly CrossChainGrpcClientController _crossChainGrpcClientController;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IBlockchainService _blockchainService;
        private bool _readyToLaunchClient;
        private int _chainId;
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
            _chainId = chainId;
            var libIdHeight = await _blockchainService.GetLibHashAndHeight();
            
            if (libIdHeight.BlockHeight > KernelConstants.GenesisBlockHeight)
            {
                // start cache if the lib is higher than genesis 
                await _crossChainDataProvider.RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
            }
            
            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.RemoteParentChainNodeIp) 
                || _grpcCrossChainConfigOption.LocalServerPort == 0) 
                return;
            
            _crossChainGrpcClientController.CreateClient(new GrpcCrossChainCommunicationContext
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                IsClientToParentChain = true,
                TargetIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                TargetPort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                LocalChainId = chainId,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort,
                ConnectionTimeout = _grpcCrossChainConfigOption.ConnectionTimeout
            });
        }

        public async Task HandleEventAsync(GrpcCrossChainRequestReceivedEvent requestReceivedEventData)
        {
            if (!await IsReadyToRequest())
                return;
            GrpcCrossChainCommunicationContext grpcCrossChainCommunicationContext =
                (GrpcCrossChainCommunicationContext) requestReceivedEventData.CrossChainCommunicationContextDto;
            grpcCrossChainCommunicationContext.LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort;
            grpcCrossChainCommunicationContext.ConnectionTimeout = _grpcCrossChainConfigOption.ConnectionTimeout;
            grpcCrossChainCommunicationContext.LocalChainId = _chainId;
            await _crossChainGrpcClientController.CreateClient(grpcCrossChainCommunicationContext);
        }
        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            if (!await IsReadyToRequest())
                return;
            _crossChainGrpcClientController.RequestCrossChainIndexing();
        }
        
        public async Task ShutdownAsync()
        {
            await _crossChainGrpcClientController.CloseClients();
        }

        public async Task<ChainInitializationContext> RequestChainInitializationContextAsync(int chainId)
        {
            string uri = string.Join(":", _grpcCrossChainConfigOption.RemoteParentChainNodeIp, _grpcCrossChainConfigOption.RemoteParentChainNodePort);
            var chainInitializationContext = await _crossChainGrpcClientController.RequestChainInitializationContext(uri, chainId, _grpcCrossChainConfigOption.ConnectionTimeout);
            return chainInitializationContext;
        }

        private async Task<bool> IsReadyToRequest()
        {
            if (!_readyToLaunchClient)
            {
                var libIdHeight = await _blockchainService.GetLibHashAndHeight();
                _readyToLaunchClient = libIdHeight.BlockHeight > KernelConstants.GenesisBlockHeight;
            }

            return _readyToLaunchClient;
        }
    }
}