using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IGrpcCrossChainPlugin, ILocalEventHandler<NewChainConnectionEvent>, ISingletonDependency
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private readonly CrossChainConfigOptions _crossChainConfigOptions;
        private int _localChainId;

        public ILogger<GrpcCrossChainClientNodePlugin> Logger { get; set; }

        public GrpcCrossChainClientNodePlugin(IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOption, 
            ICrossChainClientService crossChainClientService)
        {
            _crossChainClientService = crossChainClientService;
            _crossChainConfigOptions = crossChainConfigOption.Value;
        }

        public async Task StartAsync(int chainId)
        {
            _localChainId = chainId;
            
            if (string.IsNullOrEmpty(_crossChainConfigOptions.ParentChainId))
                return;
            Logger.LogTrace("Starting client to parent chain..");

            await _crossChainClientService.CreateClientAsync(new CrossChainClientDto
            {
                RemoteChainId = ChainHelper.ConvertBase58ToChainId(_crossChainConfigOptions.ParentChainId),
                LocalChainId = chainId,
                IsClientToParentChain = true
            });
        }

        public async Task ShutdownAsync()
        {
            await _crossChainClientService.CloseClientsAsync();
        }

        public Task HandleEventAsync(NewChainConnectionEvent eventData)
        {
            return CreateClientAsync(new CrossChainClientDto
            {
                RemoteChainId = eventData.RemoteChainId,
                RemoteServerHost = eventData.RemoteServerHost,
                RemoteServerPort = eventData.RemoteServerPort
            });
        }
        
        private async Task CreateClientAsync(CrossChainClientDto crossChainClientDto)
        {
            Logger.LogTrace(
                $"Handle cross chain request received event from chain {ChainHelper.ConvertChainIdToBase58(crossChainClientDto.RemoteChainId)}.");

            crossChainClientDto.LocalChainId = _localChainId;
            _ = _crossChainClientService.CreateClientAsync(crossChainClientDto);
        }
    }
}