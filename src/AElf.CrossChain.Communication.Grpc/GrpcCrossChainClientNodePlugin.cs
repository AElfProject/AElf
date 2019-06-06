using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientNodePlugin : IGrpcClientPlugin
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private int _localChainId;

        public ILogger<GrpcCrossChainClientNodePlugin> Logger { get; set; }

        public GrpcCrossChainClientNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption,
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption, ICrossChainClientProvider crossChainClientProvider)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _crossChainConfigOption = crossChainConfigOption.Value;
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
                RemoteChainId = _crossChainConfigOption.ParentChainId,
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