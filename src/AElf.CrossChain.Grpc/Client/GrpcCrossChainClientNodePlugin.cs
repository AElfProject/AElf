using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcCrossChainClientNodePlugin : IGrpcClientPlugin
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private readonly CrossChainConfigOptions _crossChainConfigOptions;
        public int ChainId { get; private set; }

        public ILogger<GrpcCrossChainClientNodePlugin> Logger { get; set; }

        public GrpcCrossChainClientNodePlugin(IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOption,
            ICrossChainClientService crossChainClientService)
        {
            _crossChainClientService = crossChainClientService;
            _crossChainConfigOptions = crossChainConfigOption.Value;
        }

        public async Task StartAsync(int chainId)
        {
            ChainId = chainId;

            if (string.IsNullOrEmpty(_crossChainConfigOptions.ParentChainId))
                return;
            Logger.LogInformation("Starting client to parent chain..");

            await _crossChainClientService.CreateClientAsync(new GrpcCrossChainClientCreationContext
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

        public async Task CreateClientAsync(GrpcCrossChainClientCreationContext crossChainClientCreationContext)
        {
            await _crossChainClientService.CreateClientAsync(crossChainClientCreationContext);
        }
    }
}