using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IGrpcClientPlugin
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
            
            if (_crossChainConfigOptions.ParentChainId == 0)
                return;
            Logger.LogTrace("Starting client to parent chain..");

            await _crossChainClientService.CreateClientAsync(new CrossChainClientDto
            {
                RemoteChainId = _crossChainConfigOptions.ParentChainId,
                LocalChainId = chainId,
                IsClientToParentChain = true
            });
        }

        public async Task CreateClientAsync(CrossChainClientDto crossChainClientDto)
        {
            Logger.LogTrace(
                $"Handle cross chain request received event from chain {ChainHelper.ConvertChainIdToBase58(crossChainClientDto.RemoteChainId)}.");

            crossChainClientDto.LocalChainId = _localChainId;
            _ = _crossChainClientService.CreateClientAsync(crossChainClientDto);
        }

        public async Task StopAsync()
        {
            await _crossChainClientService.CloseClientsAsync();
        }
    }
}