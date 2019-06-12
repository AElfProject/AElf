using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IGrpcClientPlugin
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOptions _crossChainConfigOptions;
        private int _localChainId;

        public ILogger<GrpcCrossChainClientNodePlugin> Logger { get; set; }

        public GrpcCrossChainClientNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption,
            IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOption, ICrossChainClientProvider crossChainClientProvider)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOptions = crossChainConfigOption.Value;
        }

        public Task StartAsync(int chainId)
        {
            _localChainId = chainId;
            
            if (string.IsNullOrEmpty(_grpcCrossChainConfigOption.RemoteParentChainServerHost)
                || _grpcCrossChainConfigOption.RemoteParentChainServerPort == 0)
                return Task.CompletedTask;
            Logger.LogTrace("Starting client to parent chain..");

            _crossChainClientProvider.CreateAndCacheClient(new CrossChainClientDto
            {
                RemoteChainId = _crossChainConfigOptions.ParentChainId,
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort,
                LocalChainId = chainId,
                IsClientToParentChain = true
            });
            return Task.CompletedTask;
        }

        public Task CreateClientAsync(CrossChainClientDto crossChainClientDto)
        {
            Logger.LogTrace(
                $"Handle cross chain request received event from chain {ChainHelpers.ConvertChainIdToBase58(crossChainClientDto.RemoteChainId)}..");
            

            _crossChainClientProvider.CreateAndCacheClient(crossChainClientDto);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await _crossChainClientProvider.CloseClientsAsync();
        }
    }
}