using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IChainInitializationPlugin, ILocalEventHandler<GrpcCrossChainRequestReceivedEvent>, ILocalEventHandler<CrossChainDataValidatedEvent>
    {
        private readonly GrpcClientProvider _grpcClientProvider;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly INewChainRegistrationService _newChainRegistrationService;
        private readonly IBlockchainService _blockchainService;
        private bool _readyToLaunchClient;
        private int _localChainId;
        
        public GrpcCrossChainClientNodePlugin(GrpcClientProvider grpcClientProvider, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, 
            INewChainRegistrationService newChainRegistrationService, IBlockchainService blockchainService)
        {
            _grpcClientProvider = grpcClientProvider;
            _newChainRegistrationService = newChainRegistrationService;
            _blockchainService = blockchainService;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
        }

        public async Task StartAsync(int chainId)
        {
            _localChainId = chainId;
            var libIdHeight = await _blockchainService.GetLibHashAndHeightAsync();
            
            if (libIdHeight.BlockHeight > Constants.GenesisBlockHeight)
            {
                // start cache if the lib is higher than genesis 
                await _newChainRegistrationService.RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
            }
            
            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.RemoteParentChainServerHost) 
                || _grpcCrossChainConfigOption.LocalServerPort == 0) 
                return;
            
            await _grpcClientProvider.CreateOrUpdateClient(new GrpcCrossChainCommunicationDto
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort,
                LocalChainId = chainId,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort,
                ConnectionTimeout = _grpcCrossChainConfigOption.ConnectionTimeout
            }, true);
        }

        public async Task HandleEventAsync(GrpcCrossChainRequestReceivedEvent requestReceivedEventData)
        {
            if (!await IsReadyToRequestAsync())
                return;
            var grpcCrossChainCommunicationDto = new GrpcCrossChainCommunicationDto
            {
                ConnectionTimeout = _grpcCrossChainConfigOption.ConnectionTimeout,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort,
                RemoteServerHost = requestReceivedEventData.RemoteServerHost,
                RemoteServerPort = requestReceivedEventData.RemoteServerPort,
                RemoteChainId = requestReceivedEventData.RemoteChainId,
                LocalChainId = _localChainId
            };

            await _grpcClientProvider.CreateOrUpdateClient(grpcCrossChainCommunicationDto,
                requestReceivedEventData.RemoteChainId == _crossChainConfigOption.ParentChainId);
        }
        
        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
        {
            if (!await IsReadyToRequestAsync())
                return;
            _grpcClientProvider.RequestCrossChainIndexing(_grpcCrossChainConfigOption.LocalServerPort);
        }
        
        public async Task ShutdownAsync()
        {
            await _grpcClientProvider.CloseClients();
        }

        public async Task<SideChainInitializationInformation> RequestChainInitializationContextAsync(int chainId)
        {
            var uriStr = new UriBuilder("http", _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                _grpcCrossChainConfigOption.RemoteParentChainServerPort).Uri.Authority;
            //string uri = string.Join(":", _grpcCrossChainConfigOption.RemoteParentChainServerHost, _grpcCrossChainConfigOption.RemoteParentChainServerPort);
            var chainInitializationContext =
                await _grpcClientProvider.RequestChainInitializationContextAsync(uriStr, chainId,
                    _grpcCrossChainConfigOption.ConnectionTimeout);
            return chainInitializationContext;
        }

        private async Task<bool> IsReadyToRequestAsync()
        {
            if (!_readyToLaunchClient)
            {
                var libIdHeight = await _blockchainService.GetLibHashAndHeightAsync();
                _readyToLaunchClient = libIdHeight.BlockHeight > Constants.GenesisBlockHeight;
            }

            return _readyToLaunchClient;
        }
    }
}