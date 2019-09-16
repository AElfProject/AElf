using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
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
            var port = 5000;

            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };

            await _crossChainClientService.CreateClientAsync(fakeCrossChainClient);
            var res = _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out _);
            Assert.True(res);
        }
        
        [Fact]
        public async Task CloseClient_Test()
        {
            var remoteChainId = ChainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);
            
            var host = "127.0.0.1";
            var port = 5010;
            await Server.StartAsync(host, port);

            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };

            await _crossChainClientService.CreateClientAsync(fakeCrossChainClient);
            _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out var client);
            await client.ConnectAsync();
            Assert.True(client.IsConnected);
            await _crossChainClientService.CloseClientsAsync();
            Assert.False(client.IsConnected);

            Server.Dispose();
        }
    }
}