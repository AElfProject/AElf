using System;
using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IGrpcCrossChainPlugin, ILocalEventHandler<GrpcCrossChainRequestReceivedEvent>
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        
//        private readonly INewChainRegistrationService _newChainRegistrationService;
//        private readonly IBlockchainService _blockchainService;
//        private bool _readyToLaunchClient;
        private int _localChainId;

        public GrpcCrossChainClientNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption,
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption,
            INewChainRegistrationService newChainRegistrationService, IBlockchainService blockchainService, 
            ICrossChainClientProvider crossChainClientProvider)
        {
//            _newChainRegistrationService = newChainRegistrationService;
//            _blockchainService = blockchainService;
            _crossChainClientProvider = crossChainClientProvider;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
        }

        public async Task StartAsync(int chainId)
        {
            _localChainId = chainId;
//            var libIdHeight = await _blockchainService.GetLibHashAndHeightAsync();
//
//            if (libIdHeight.BlockHeight > Constants.GenesisBlockHeight)
//            {
//                // start cache if the lib is higher than genesis 
//                await _newChainRegistrationService.RegisterNewChainsAsync(libIdHeight.BlockHash,
//                    libIdHeight.BlockHeight);
//            }

            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.RemoteParentChainServerHost)
                || _grpcCrossChainConfigOption.LocalServerPort == 0)
                return;

            await _crossChainClientProvider.CreateAndCacheClientAsync(new GrpcCrossChainClientDto
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort,
                LocalChainId = chainId,
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort
            });
        }

        public async Task HandleEventAsync(GrpcCrossChainRequestReceivedEvent requestReceivedEventData)
        {
//            if (!await IsReadyToRequestAsync())
//                return;
            var grpcCrossChainCommunicationDto = new GrpcCrossChainClientDto
            {
                LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort,
                RemoteServerHost = requestReceivedEventData.RemoteServerHost,
                RemoteServerPort = requestReceivedEventData.RemoteServerPort,
                RemoteChainId = requestReceivedEventData.RemoteChainId,
                LocalChainId = _localChainId,
                IsClientToParentChain = requestReceivedEventData.RemoteChainId == _crossChainConfigOption.ParentChainId
            };

            await _crossChainClientProvider.CreateAndCacheClientAsync(grpcCrossChainCommunicationDto);
        }

//        public async Task HandleEventAsync(CrossChainDataValidatedEvent eventData)
//        {
//            if (!await IsReadyToRequestAsync())
//                return;
//            _grpcCrossChainClientProvider.RequestCrossChainIndexing(_grpcCrossChainConfigOption.LocalServerPort);
//        }

        public async Task StopAsync()
        {
            await _crossChainClientProvider.CloseClientsAsync();
        }

//        public async Task<ByteString> RequestChainInitializationContextAsync(int chainId)
//        {
//            var uriStr = new UriBuilder("http", _grpcCrossChainConfigOption.RemoteParentChainServerHost,
//                _grpcCrossChainConfigOption.RemoteParentChainServerPort).Uri.Authority;
//            //string uri = string.Join(":", _grpcCrossChainConfigOption.RemoteParentChainServerHost, _grpcCrossChainConfigOption.RemoteParentChainServerPort);
//            var chainInitializationContext =
//                await _grpcCrossChainClientProvider.RequestChainInitializationContextAsync(uriStr, chainId,
//                    _grpcCrossChainConfigOption.ConnectionTimeout);
//            return chainInitializationContext.ToByteString();
//        }

//        private async Task<bool> IsReadyToRequestAsync()
//        {
//            if (!_readyToLaunchClient)
//            {
//                var libIdHeight = await _blockchainService.GetLibHashAndHeightAsync();
//                _readyToLaunchClient = libIdHeight.BlockHeight > Constants.GenesisBlockHeight;
//            }
//
//            return _readyToLaunchClient;
//        }
    }
}