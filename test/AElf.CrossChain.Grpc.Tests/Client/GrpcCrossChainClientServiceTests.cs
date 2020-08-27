using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using Xunit;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcCrossChainClientServiceTests : GrpcCrossChainClientTestBase
    {
        private readonly ICrossChainClientService _crossChainClientService;
        private readonly ICrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcCrossChainClientServiceTests()
        {
            _crossChainClientService = GetRequiredService<ICrossChainClientService>();
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
        }
        
        [Fact]
        public async Task CreateClient_Test()
        {
            var remoteChainId = ChainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);
            
            var host = "127.0.0.1";
            var port = 5100;
            await Server.StartAsync(port);

            var fakeCrossChainClient = new CrossChainClientCreationContext
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };

            await _crossChainClientService.CreateClientAsync(fakeCrossChainClient);
            var res = _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out var client);
            Assert.True(res);
            
            await client.ConnectAsync();
            Assert.True(client.IsConnected);

            await client.CloseAsync();
            Assert.False(client.IsConnected);
        }
    }
}