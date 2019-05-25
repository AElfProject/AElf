using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IGrpcClientPlugin
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        
//        private readonly INewChainRegistrationService _newChainRegistrationService;
//        private readonly IBlockchainService _blockchainService;
//        private bool _readyToLaunchClient;
        private int _localChainId;

        public ILogger<GrpcCrossChainClientNodePlugin> Logger { get; set; }

        public GrpcCrossChainClientNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption,
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, ICrossChainClientProvider crossChainClientProvider)
        {
//            _newChainRegistrationService = newChainRegistrationService;
//            _blockchainService = blockchainService;
            _crossChainClientProvider = crossChainClientProvider;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
        }

        public Task StartAsync(int chainId)
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
            Logger.LogTrace("Starting client to parent chain..");
            
            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.RemoteParentChainServerHost)
                || _grpcCrossChainConfigOption.LocalServerPort == 0)
                return Task.CompletedTask;

            _crossChainClientProvider.CreateAndCacheClient(new GrpcCrossChainClientDto
            {
                RemoteChainId = _crossChainConfigOption.ParentChainId,
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort,
                LocalChainId = chainId,
                IsClientToParentChain = true
            });
            return Task.CompletedTask;
        }

        public Task CreateClientAsync(GrpcCrossChainClientDto grpcCrossChainClientDto)
        {
//            if (!await IsReadyToRequestAsync())
//                return;
            Logger.LogTrace(
                $"Handle cross chain request received event from chain {ChainHelpers.ConvertChainIdToBase58(grpcCrossChainClientDto.RemoteChainId)}..");
            
            var grpcCrossChainCommunicationDto = new GrpcCrossChainClientDto
            {
                RemoteServerHost = grpcCrossChainClientDto.RemoteServerHost,
                RemoteServerPort = grpcCrossChainClientDto.RemoteServerPort,
                RemoteChainId = grpcCrossChainClientDto.RemoteChainId,
                LocalChainId = _localChainId,
                IsClientToParentChain = grpcCrossChainClientDto.RemoteChainId == _crossChainConfigOption.ParentChainId
            };

            _crossChainClientProvider.CreateAndCacheClient(grpcCrossChainCommunicationDto);
            return Task.CompletedTask;
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